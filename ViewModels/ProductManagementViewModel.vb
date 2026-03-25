Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports SmartTill.V2.Models
Imports SmartTill.V2.Services
Imports System.Collections.ObjectModel
Imports System.Linq

Namespace ViewModels
    Public Class ProductManagementViewModel
        Inherits ObservableObject

        Private ReadOnly _dbService As DatabaseService

        Private _selectedTabIndex As Integer = 1 ' Default to Product Management
        Public Property SelectedTabIndex As Integer
            Get
                Return _selectedTabIndex
            End Get
            Set(ByVal value As Integer)
                SetProperty(_selectedTabIndex, value)
            End Set
        End Property

        Private _products As ObservableCollection(Of Product)
        Public Property Products As ObservableCollection(Of Product)
            Get
                Return _products
            End Get
            Set(ByVal value As ObservableCollection(Of Product))
                SetProperty(_products, value)
            End Set
        End Property

        Private _categories As ObservableCollection(Of Category)
        Public Property Categories As ObservableCollection(Of Category)
            Get
                Return _categories
            End Get
            Set(ByVal value As ObservableCollection(Of Category))
                SetProperty(_categories, value)
            End Set
        End Property

        Private _selectedProduct As Product
        Public Property SelectedProduct As Product
            Get
                Return _selectedProduct
            End Get
            Set(ByVal value As Product)
                SetProperty(_selectedProduct, value)
                DeleteProductCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        ' Form Properties
        Private _isAddFormVisible As Boolean = False
        Public Property IsAddFormVisible As Boolean
            Get
                Return _isAddFormVisible
            End Get
            Set(ByVal value As Boolean)
                SetProperty(_isAddFormVisible, value)
            End Set
        End Property

        Private _newName As String
        Public Property NewName As String
            Get
                Return _newName
            End Get
            Set(ByVal value As String)
                SetProperty(_newName, value)
            End Set
        End Property

        Private _newBarcode As String
        Public Property NewBarcode As String
            Get
                Return _newBarcode
            End Get
            Set(ByVal value As String)
                SetProperty(_newBarcode, value)
            End Set
        End Property

        Private _newPrice As Decimal
        Public Property NewPrice As Decimal
            Get
                Return _newPrice
            End Get
            Set(ByVal value As Decimal)
                SetProperty(_newPrice, value)
            End Set
        End Property

        Private _newStock As Integer
        Public Property NewStock As Integer
            Get
                Return _newStock
            End Get
            Set(ByVal value As Integer)
                SetProperty(_newStock, value)
            End Set
        End Property

        Private _newCategory As String
        Public Property NewCategory As String
            Get
                Return _newCategory
            End Get
            Set(ByVal value As String)
                SetProperty(_newCategory, value)
            End Set
        End Property

        Private _newCategoryName As String
        Public Property NewCategoryName As String
            Get
                Return _newCategoryName
            End Get
            Set(ByVal value As String)
                SetProperty(_newCategoryName, value)
            End Set
        End Property

        Private _errorMessage As String
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(ByVal value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        ' Commands
        Public Property ToggleAddFormCommand As IRelayCommand
        Public Property SaveProductCommand As IRelayCommand
        Public Property DeleteProductCommand As IRelayCommand
        Public Property RefreshCommand As IRelayCommand
        Public Property AddCategoryCommand As IRelayCommand
        Public Property DeleteCategoryCommand As IRelayCommand

        Public Sub New()
            _dbService = New DatabaseService()
            LoadProducts()
            LoadCategories()
            
            ToggleAddFormCommand = New RelayCommand(AddressOf ToggleForm)
            SaveProductCommand = New RelayCommand(AddressOf SaveProduct)
            DeleteProductCommand = New RelayCommand(AddressOf DeleteProduct, Function() SelectedProduct IsNot Nothing)
            RefreshCommand = New RelayCommand(AddressOf LoadProducts)
            AddCategoryCommand = New RelayCommand(AddressOf AddCategory)
            DeleteCategoryCommand = New RelayCommand(Of Category)(AddressOf DeleteCategory)
        End Sub

        Private Sub LoadProducts()
            Dim list = _dbService.GetAllProducts()
            Products = New ObservableCollection(Of Product)(list)
        End Sub

        Private Sub LoadCategories()
            Dim list = _dbService.GetAllCategories()
            Categories = New ObservableCollection(Of Category)(list)
        End Sub

        Private Sub ToggleForm()
            IsAddFormVisible = Not IsAddFormVisible
            ErrorMessage = String.Empty
            If Not IsAddFormVisible Then
                ClearForm()
            End If
        End Sub

        Private Sub ClearForm()
            NewName = String.Empty
            NewBarcode = String.Empty
            NewPrice = 0
            NewStock = 0
            NewCategory = String.Empty
        End Sub

        Private Sub SaveProduct()
            If String.IsNullOrWhiteSpace(NewName) OrElse String.IsNullOrWhiteSpace(NewBarcode) OrElse String.IsNullOrWhiteSpace(NewCategory) Then
                ErrorMessage = "Name, Barcode, and Category are required."
                Return
            End If

            If _dbService.CreateProduct(NewName, NewBarcode, NewPrice, NewStock, NewCategory) Then
                LoadProducts()
                ToggleForm()
            Else
                ErrorMessage = "Failed to save product. Barcode might already exist."
            End If
        End Sub

        Private Sub AddCategory()
            If Not String.IsNullOrWhiteSpace(NewCategoryName) Then
                If _dbService.CreateCategory(NewCategoryName) Then
                    LoadCategories()
                    NewCategoryName = String.Empty
                End If
            End If
        End Sub

        Private Sub DeleteCategory(category As Category)
            If category IsNot Nothing Then
                If _dbService.DeleteCategory(category.Id) Then
                    LoadCategories()
                End If
            End If
        End Sub

        Private Sub DeleteProduct()
            If SelectedProduct IsNot Nothing Then
                If _dbService.DeleteProduct(SelectedProduct.Id) Then
                    Products.Remove(SelectedProduct)
                End If
            End If
        End Sub
    End Class
End Namespace