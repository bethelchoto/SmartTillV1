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

        ' Cart management
        Public Class CartItem
            Public Property Product As Product
            Public Property Quantity As Integer
            Public Property Subtotal As Decimal
        End Class

        Private _cartItems As ObservableCollection(Of CartItem)
        Public Property CartItems As ObservableCollection(Of CartItem)
            Get
                Return _cartItems
            End Get
            Set(ByVal value As ObservableCollection(Of CartItem))
                SetProperty(_cartItems, value)
                OnPropertyChanged(NameOf(TotalAmount))
            End Set
        End Property

        Public ReadOnly Property TotalAmount As Decimal
            Get
                Return CartItems.Sum(Function(i) i.Subtotal)
            End Get
        End Property

        Private _searchText As String
        Public Property SearchText As String
            Get
                Return _searchText
            End Get
            Set(ByVal value As String)
                SetProperty(_searchText, value)
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
        Public Property AddItemCommand As IRelayCommand(Of Product)
        Public Property RemoveItemCommand As IRelayCommand(Of CartItem)
        Public Property CheckoutCommand As IRelayCommand
        Public Property SearchCommand As IRelayCommand

        Public Sub New(cashierId As Integer)
            _dbService = New DatabaseService()
            _cashierId = cashierId
            CartItems = New ObservableCollection(Of CartItem)()
            LoadInventory()

            AddItemCommand = New RelayCommand(Of Product)(AddressOf AddToCart)
            RemoveItemCommand = New RelayCommand(Of CartItem)(AddressOf RemoveFromCart)
            CheckoutCommand = New RelayCommand(AddressOf ExecuteCheckout, Function() CartItems.Count > 0)
            SearchCommand = New RelayCommand(AddressOf LoadInventory)
        End Sub

        Private Sub LoadInventory()
            Dim all = _dbService.GetAllProducts()
            If String.IsNullOrWhiteSpace(SearchText) Then
                Inventory = all
            Else
                Inventory = all.Where(Function(p) p.Name.ToLower().Contains(SearchText.ToLower()) OrElse p.Barcode.Contains(SearchText)).ToList()
            End If
        End Sub

        Private Sub AddToCart(product As Product)
            If product Is Nothing OrElse product.StockQuantity <= 0 Then
                StatusMessage = "Item out of stock!"
                Return
            End If

            Dim existing = CartItems.FirstOrDefault(Function(i) i.Product.Id = product.Id)
            If existing IsNot Nothing Then
                existing.Quantity += 1
                existing.Subtotal = existing.Quantity * existing.Product.Price
            Else
                CartItems.Add(New CartItem With {
                    .Product = product,
                    .Quantity = 1,
                    .Subtotal = product.Price
                })
            End If
            
            OnPropertyChanged(NameOf(TotalAmount))
            CheckoutCommand.NotifyCanExecuteChanged()
            StatusMessage = $"Added {product.Name} to cart."
        End Sub

        Private Sub RemoveFromCart(item As CartItem)
            If item IsNot Nothing Then
                CartItems.Remove(item)
                OnPropertyChanged(NameOf(TotalAmount))
                CheckoutCommand.NotifyCanExecuteChanged()
            End If
        End Sub

        Private Sub ExecuteCheckout()
            Dim sale = New Sale With {
                .CashierId = _cashierId,
                .TotalAmount = TotalAmount,
                .SaleDate = DateTime.Now
            }

            Dim details = CartItems.Select(Function(i) New SaleDetail With {
                .ProductId = i.Product.Id,
                .Quantity = i.Quantity,
                .UnitPrice = i.Product.Price
            }).ToList()

            If _dbService.ProcessSale(sale, details) Then
                StatusMessage = "Sale processed successfully!"
                CartItems.Clear()
                LoadInventory()
                OnPropertyChanged(NameOf(TotalAmount))
                CheckoutCommand.NotifyCanExecuteChanged()
            Else
                StatusMessage = "Transaction failed. Please try again."
            End If
        End Sub
    End Class
End Namespace