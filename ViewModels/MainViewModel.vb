Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports SmartTill.V2.Services
Imports SmartTill.V2.Models

Namespace ViewModels
    Public Class MainViewModel
        Inherits ObservableObject

        Private ReadOnly _dbService As DatabaseService
        
        Private _currentUser As User
        Public Property CurrentUser As User
            Get
                Return _currentUser
            End Get
            Set(ByVal value As User)
                SetProperty(_currentUser, value)
                OnPropertyChanged(NameOf(UserDisplayName))
                OnPropertyChanged(NameOf(UserRoleDisplay))
                OnPropertyChanged(NameOf(IsAdmin))
                OnPropertyChanged(NameOf(IsManager))
                OnPropertyChanged(NameOf(IsTailor))
            End Set
        End Property

        Public ReadOnly Property UserDisplayName As String
            Get
                Return If(CurrentUser?.Username, "Guest")
            End Get
        End Property

        Public ReadOnly Property UserRoleDisplay As String
            Get
                Return If(CurrentUser?.Role, "Unknown")
            End Get
        End Property

        Public ReadOnly Property IsAdmin As Boolean
            Get
                Return CurrentUser?.Role = "Admin"
            End Get
        End Property

        Public ReadOnly Property IsManager As Boolean
            Get
                ' Managers and Admins can see management stuff
                Return CurrentUser?.Role = "Manager" OrElse IsAdmin
            End Get
        End Property

        Public ReadOnly Property IsTailor As Boolean
            Get
                Return CurrentUser?.Role = "Tailor"
            End Get
        End Property

        Private _currentView As Object
        Public Property CurrentView As Object
            Get
                Return _currentView
            End Get
            Set(ByVal value As Object)
                SetProperty(_currentView, value)
            End Set
        End Property

        Private _status As String = "Ready"
        Public Property Status As String
            Get
                Return _status
            End Get
            Set(ByVal value As String)
                SetProperty(_status, value)
            End Set
        End Property

        Private _isProductsMenuExpanded As Boolean = False
        Public Property IsProductsMenuExpanded As Boolean
            Get
                Return _isProductsMenuExpanded
            End Get
            Set(ByVal value As Boolean)
                SetProperty(_isProductsMenuExpanded, value)
            End Set
        End Property

        Public Property ToggleProductsMenuCommand As IRelayCommand
        Public Property ShowCategoryManagementCommand As IRelayCommand
        Public Property ShowDashboardCommand As IRelayCommand
        Public Property ShowProductManagementCommand As IRelayCommand
        Public Property ShowUserManagementCommand As IRelayCommand
        Public Property ShowPOSCommand As IRelayCommand
        Public Property LogoutCommand As IRelayCommand

        Public Sub New()
            _dbService = New DatabaseService()
            
            ' Default view
            ' CurrentView = New DashboardViewModel() ' To be implemented

            ShowDashboardCommand = New RelayCommand(AddressOf ShowDashboard)
            ShowProductManagementCommand = New RelayCommand(AddressOf ShowProductManagement)
            ShowUserManagementCommand = New RelayCommand(AddressOf ShowUserManagement)
            ShowCategoryManagementCommand = New RelayCommand(AddressOf ShowCategoryManagement)
            ShowPOSCommand = New RelayCommand(AddressOf ShowPOS)
            LogoutCommand = New RelayCommand(AddressOf Logout)
            ToggleProductsMenuCommand = New RelayCommand(Sub() IsProductsMenuExpanded = Not IsProductsMenuExpanded)
        End Sub

        Private Sub ShowDashboard()
            CurrentView = Nothing
        End Sub

        Private Sub ShowProductManagement()
            Dim vm = New ProductManagementViewModel()
            vm.SelectedTabIndex = 1 ' Product Management Tab
            CurrentView = vm
        End Sub

        Private Sub ShowCategoryManagement()
            Dim vm = New ProductManagementViewModel()
            vm.SelectedTabIndex = 0 ' Category Management Tab
            CurrentView = vm
        End Sub

        Private Sub ShowUserManagement()
            CurrentView = New UserManagementViewModel()
        End Sub

        Private Sub ShowPOS()
            CurrentView = New POSViewModel(CurrentUser.Id)
        End Sub

        Private Sub Logout()
            ' Logic to return to LoginWindow
            Dim loginWin = New LoginWindow()
            loginWin.Show()
            
            ' Close current MainWindow
            For Each win In System.Windows.Application.Current.Windows
                If TypeOf win Is MainWindow Then
                    win.Close()
                    Exit For
                End If
            Next
        End Sub
    End Class
End Namespace