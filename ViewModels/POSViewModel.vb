Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports SmartTill.V2.Models
Imports SmartTill.V2.Services
Imports System.Collections.ObjectModel
Imports System.Linq

Namespace ViewModels
    Public Class POSViewModel
        Inherits ObservableObject

        Private ReadOnly _dbService As DatabaseService
        Private _cashierId As Integer

        ' Financials
        Private _subtotal As Decimal
        Public Property Subtotal As Decimal
            Get
                Return _subtotal
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_subtotal, value)
                CalculateTotals()
            End Set
        End Property

        Private _discountRate As Decimal = 0
        Public Property DiscountRate As Decimal
            Get
                Return _discountRate
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_discountRate, value)
                CalculateTotals()
            End Set
        End Property

        Private _taxAmount As Decimal
        Public Property TaxAmount As Decimal
            Get
                Return _taxAmount
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_taxAmount, value)
            End Set
        End Property

        Private _totalAmount As Decimal
        Public Property TotalAmount As Decimal
            Get
                Return _totalAmount
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_totalAmount, value)
            End Set
        End Property

        Private _amountPaid As Decimal
        Public Property AmountPaid As Decimal
            Get
                Return _amountPaid
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_amountPaid, value)
                CalculateChange()
            End Set
        End Property

        Private _changeDue As Decimal
        Public Property ChangeDue As Decimal
            Get
                Return _changeDue
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_changeDue, value)
            End Set
        End Property

        ' Payment & Billing
        Public Property PaymentMethods As String() = {"Cash", "Card", "EcoCash", "Innbuck", "Bank Transfer"}
        
        Private _selectedPaymentMethod As String = "Cash"
        Public Property SelectedPaymentMethod As String
            Get
                Return _selectedPaymentMethod
            End Get
            Set(ByVal value As String)
                SetProperty(_selectedPaymentMethod, value)
            End Set
        End Property

        Private _customerName As String = "Walk-in Customer"
        Public Property CustomerName As String
            Get
                Return _customerName
            End Get
            Set(ByVal value As String)
                SetProperty(_customerName, value)
            End Set
        End Property

        ' Item Entry
        Private _barcodeInput As String
        Public Property BarcodeInput As String
            Get
                Return _barcodeInput
            End Get
            Set(ByVal value As String)
                SetProperty(_barcodeInput, value)
            End Set
        End Property

        Private _cartItems As ObservableCollection(Of SaleDetail)
        Public Property CartItems As ObservableCollection(Of SaleDetail)
            Get
                Return _cartItems
            End Get
            Set(ByVal value As ObservableCollection(Of SaleDetail))
                SetProperty(_cartItems, value)
                UpdateSubtotal()
            End Set
        End Property

        Private _inventory As List(Of Product)
        Public Property Inventory As List(Of Product)
            Get
                Return _inventory
            End Get
            Set(ByVal value As List(Of Product))
                SetProperty(_inventory, value)
            End Set
        End Property

        Private _statusMessage As String
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(ByVal value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        ' Commands
        Public Property QuickAddCommand As IRelayCommand
        Public Property AddItemCommand As IRelayCommand(Of Product)
        Public Property RemoveItemCommand As IRelayCommand(Of SaleDetail)
        Public Property CheckoutCommand As IRelayCommand
        Public Property AbortCommand As IRelayCommand

        Public Sub New(cashierId As Integer)
            _dbService = New DatabaseService()
            _cashierId = cashierId
            CartItems = New ObservableCollection(Of SaleDetail)()
            LoadInventory()

            QuickAddCommand = New RelayCommand(AddressOf AddByBarcode)
            AddItemCommand = New RelayCommand(Of Product)(AddressOf AddToCart)
            RemoveItemCommand = New RelayCommand(Of SaleDetail)(AddressOf RemoveFromCart)
            CheckoutCommand = New RelayCommand(AddressOf ExecuteCheckout, Function() CartItems.Count > 0)
            AbortCommand = New RelayCommand(AddressOf ClearAll)
        End Sub

        Private Sub LoadInventory()
            Inventory = _dbService.GetAllProducts()
        End Sub

        Private Sub AddByBarcode()
            If String.IsNullOrWhiteSpace(BarcodeInput) Then Return
            
            Dim product = _dbService.GetProductByBarcode(BarcodeInput)
            If product IsNot Nothing Then
                AddToCart(product)
                BarcodeInput = String.Empty
            Else
                StatusMessage = "Product not found!"
            End If
        End Sub

        Private Sub AddToCart(product As Product)
            If product Is Nothing OrElse product.StockQuantity <= 0 Then
                StatusMessage = "Item out of stock!"
                Return
            End If

            Dim existing = CartItems.FirstOrDefault(Function(i) i.ProductId = product.Id)
            If existing IsNot Nothing Then
                existing.Quantity += 1
                existing.Total = existing.Quantity * existing.UnitPrice * (1 - (existing.DiscountPercent / 100))
            Else
                CartItems.Add(New SaleDetail With {
                    .ProductId = product.Id,
                    .ProductName = product.Name,
                    .Quantity = 1,
                    .UnitPrice = product.Price,
                    .DiscountPercent = 0,
                    .Total = product.Price
                })
            End If
            
            UpdateSubtotal()
            CheckoutCommand.NotifyCanExecuteChanged()
            StatusMessage = $"Added {product.Name} to cart."
        End Sub

        Private Sub RemoveFromCart(item As SaleDetail)
            If item IsNot Nothing Then
                CartItems.Remove(item)
                UpdateSubtotal()
                CheckoutCommand.NotifyCanExecuteChanged()
            End If
        End Sub

        Private Sub UpdateSubtotal()
            Subtotal = CartItems.Sum(Function(i) i.Total)
        End Sub

        Private Sub CalculateTotals()
            Dim discount = Subtotal * (DiscountRate / 100)
            Dim taxableAmount = Subtotal - discount
            TaxAmount = taxableAmount * 0.15D ' Assuming 15% VAT
            TotalAmount = taxableAmount + TaxAmount
            CalculateChange()
        End Sub

        Private Sub CalculateChange()
            If AmountPaid >= TotalAmount Then
                ChangeDue = AmountPaid - TotalAmount
            Else
                ChangeDue = 0
            End If
        End Sub

        Private Sub ClearAll()
            CartItems.Clear()
            UpdateSubtotal()
            AmountPaid = 0
            DiscountRate = 0
            CustomerName = "Walk-in Customer"
            StatusMessage = "Transaction aborted."
        End Sub

        Private Sub ExecuteCheckout()
            If AmountPaid < TotalAmount AndAlso SelectedPaymentMethod = "Cash" Then
                StatusMessage = "Insufficient cash payment!"
                Return
            End If

            Dim sale = New Sale With {
                .CashierId = _cashierId,
                .Subtotal = Subtotal,
                .DiscountAmount = Subtotal * (DiscountRate / 100),
                .TaxAmount = TaxAmount,
                .TotalAmount = TotalAmount,
                .AmountPaid = AmountPaid,
                .ChangeDue = ChangeDue,
                .PaymentMethod = SelectedPaymentMethod,
                .CustomerName = CustomerName,
                .SaleDate = DateTime.Now
            }

            If _dbService.ProcessSale(sale, CartItems.ToList()) Then
                StatusMessage = "Sale processed successfully!"
                ' In a real app, you'd trigger receipt printing here
                ClearAll()
                LoadInventory()
            Else
                StatusMessage = "Transaction failed. Database error."
            End If
        End Sub
    End Class
End Namespace