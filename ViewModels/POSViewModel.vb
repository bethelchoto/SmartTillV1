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
        Private _lastSaleId As Integer

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

        ' Receipt Toggle
        Private _isReceiptVisible As Boolean
        Public Property IsReceiptVisible As Boolean
            Get
                Return _isReceiptVisible
            End Get
            Set(ByVal value As Boolean)
                SetProperty(_isReceiptVisible, value)
            End Set
        End Property

        Private _lastSale As Sale
        Public Property LastSale As Sale
            Get
                Return _lastSale
            End Get
            Set(ByVal value As Sale)
                SetProperty(_lastSale, value)
            End Set
        End Property

        Private _lastSaleDetails As List(Of SaleDetail)
        Public Property LastSaleDetails As List(Of SaleDetail)
            Get
                Return _lastSaleDetails
            End Get
            Set(ByVal value As List(Of SaleDetail))
                SetProperty(_lastSaleDetails, value)
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
                If SetProperty(_barcodeInput, value) Then
                    UpdateFilteredInventory()
                    IsSearchDropdownOpen = Not String.IsNullOrWhiteSpace(value)
                End If
            End Set
        End Property

        Private _isSearchDropdownOpen As Boolean
        Public Property IsSearchDropdownOpen As Boolean
            Get
                Return _isSearchDropdownOpen
            End Get
            Set(ByVal value As Boolean)
                SetProperty(_isSearchDropdownOpen, value)
            End Set
        End Property

        Private _filteredInventory As ObservableCollection(Of Product)
        Public Property FilteredInventory As ObservableCollection(Of Product)
            Get
                Return _filteredInventory
            End Get
            Set(ByVal value As ObservableCollection(Of Product))
                SetProperty(_filteredInventory, value)
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
        Public Property IncrementQtyCommand As IRelayCommand(Of SaleDetail)
        Public Property DecrementQtyCommand As IRelayCommand(Of SaleDetail)
        Public Property CheckoutCommand As IRelayCommand
        Public Property AbortCommand As IRelayCommand
        Public Property HoldCommand As IRelayCommand
        Public Property RecallCommand As IRelayCommand(Of Integer)
        Public Property CloseReceiptCommand As IRelayCommand

        Private _heldSales As ObservableCollection(Of Object)
        Public Property HeldSales As ObservableCollection(Of Object)
            Get
                Return _heldSales
            End Get
            Set(ByVal value As ObservableCollection(Of Object))
                SetProperty(_heldSales, value)
            End Set
        End Property

        Public Sub New(cashierId As Integer)
            _dbService = New DatabaseService()
            _cashierId = cashierId
            CartItems = New ObservableCollection(Of SaleDetail)()
            FilteredInventory = New ObservableCollection(Of Product)()
            LoadInventory()

            QuickAddCommand = New RelayCommand(AddressOf AddByBarcode)
            AddItemCommand = New RelayCommand(Of Product)(AddressOf AddToCart)
            RemoveItemCommand = New RelayCommand(Of SaleDetail)(AddressOf RemoveFromCart)
            IncrementQtyCommand = New RelayCommand(Of SaleDetail)(AddressOf IncrementQty)
            DecrementQtyCommand = New RelayCommand(Of SaleDetail)(AddressOf DecrementQty)
            CheckoutCommand = New RelayCommand(AddressOf ExecuteCheckout, Function() CartItems.Count > 0)
            AbortCommand = New RelayCommand(AddressOf ClearAll)
            HoldCommand = New RelayCommand(AddressOf ExecuteHold, Function() CartItems.Count > 0)
            RecallCommand = New RelayCommand(Of Integer)(AddressOf ExecuteRecall)
            CloseReceiptCommand = New RelayCommand(AddressOf CloseReceipt)
            
            HeldSales = New ObservableCollection(Of Object)()
            LoadHeldSales()
        End Sub

        Private Sub UpdateFilteredInventory()
            If String.IsNullOrWhiteSpace(BarcodeInput) Then
                FilteredInventory.Clear()
                Return
            End If

            Dim query = BarcodeInput.ToLower()
            Dim results = Inventory.Where(Function(p) p.Name.ToLower().Contains(query) OrElse p.Barcode.ToLower().Contains(query)).Take(5).ToList()
            
            FilteredInventory.Clear()
            For Each p In results
                FilteredInventory.Add(p)
            Next
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
            IsSearchDropdownOpen = False
            BarcodeInput = String.Empty
        End Sub

        Private Sub IncrementQty(item As SaleDetail)
            If item IsNot Nothing Then
                item.Quantity += 1
                item.Total = item.Quantity * item.UnitPrice * (1 - (item.DiscountPercent / 100))
                UpdateSubtotal()
            End If
        End Sub

        Private Sub DecrementQty(item As SaleDetail)
            If item IsNot Nothing AndAlso item.Quantity > 1 Then
                item.Quantity -= 1
                item.Total = item.Quantity * item.UnitPrice * (1 - (item.DiscountPercent / 100))
                UpdateSubtotal()
            End If
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
            If CartItems.Count = 0 Then
                StatusMessage = "Cannot process an empty sale!"
                Return
            End If

            ' For Cash: if no amount entered, default to exact amount (no change)
            ' For Card/EcoCash: no amount required
            If SelectedPaymentMethod = "Cash" AndAlso AmountPaid = 0 Then
                AmountPaid = TotalAmount
            ElseIf SelectedPaymentMethod <> "Cash" Then
                AmountPaid = TotalAmount
                ChangeDue = 0
            ElseIf AmountPaid < TotalAmount Then
                StatusMessage = "Insufficient cash amount!"
                Return
            End If

            ' Store details for receipt BEFORE clearing
            LastSaleDetails = CartItems.ToList()

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
            
            LastSale = sale
            LastSale.SaleDate = DateTime.Now

            ' Always show the receipt so the cashier sees the transaction
            IsReceiptVisible = True

            ' Save to DB and reduce stock
            Dim result = _dbService.ProcessSale(sale, LastSaleDetails)
            If result.Id > 0 Then
                LastSale.Id = result.Id
                StatusMessage = "Sale processed successfully! Stock updated."
                LoadInventory()
            Else
                StatusMessage = $"DB Error: {result.ErrorMsg}"
            End If
        End Sub

        Private Sub CloseReceipt()
            IsReceiptVisible = False
            ClearAll()
        End Sub

        Private Sub LoadHeldSales()
            Dim held = _dbService.GetHeldSales()
            HeldSales.Clear()
            For Each h In held
                HeldSales.Add(h)
            Next
        End Sub

        Private Sub ExecuteHold()
            Dim sale = New Sale With {
                .CashierId = _cashierId,
                .Subtotal = Subtotal,
                .DiscountAmount = Subtotal * (DiscountRate / 100),
                .TaxAmount = TaxAmount,
                .TotalAmount = TotalAmount,
                .CustomerName = CustomerName
            }

            Dim ref = $"HOLD-{DateTime.Now:HHmm}"
            If _dbService.HoldSale(sale, CartItems.ToList(), ref) Then
                StatusMessage = $"Sale held with reference: {ref}"
                ClearAll()
                LoadHeldSales()
            Else
                StatusMessage = "Failed to hold sale."
            End If
        End Sub

        Private Sub ExecuteRecall(heldId As Integer)
            ' Using heldId from the object in HeldSales
            Dim result = _dbService.RecallHeldSale(heldId)
            If result.Item1 IsNot Nothing Then
                ClearAll()
                CustomerName = result.Item1.CustomerName
                DiscountRate = If(result.Item1.Subtotal > 0, (result.Item1.DiscountAmount / result.Item1.Subtotal) * 100, 0)
                
                For Each d In result.Item2
                    CartItems.Add(d)
                Next
                UpdateSubtotal()
                LoadHeldSales()
                StatusMessage = "Sale recalled."
            End If
        End Sub
    End Class
End Namespace