Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports SmartTill.V2.Models
Imports SmartTill.V2.Services
Imports System.Collections.ObjectModel
Imports System.Windows.Controls
Imports System.Linq

Namespace ViewModels
    Public Class UserManagementViewModel
        Inherits ObservableObject

        Private ReadOnly _dbService As DatabaseService

        Private _users As ObservableCollection(Of User)
        Public Property Users As ObservableCollection(Of User)
            Get
                Return _users
            End Get
            Set(ByVal value As ObservableCollection(Of User))
                SetProperty(_users, value)
            End Set
        End Property

        Private _selectedUser As User
        Public Property SelectedUser As User
            Get
                Return _selectedUser
            End Get
            Set(ByVal value As User)
                SetProperty(_selectedUser, value)
                DeleteUserCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        ' Form Properties
        Private _isAddUserFormVisible As Boolean = False
        Public Property IsAddUserFormVisible As Boolean
            Get
                Return _isAddUserFormVisible
            End Get
            Set(ByVal value As Boolean)
                SetProperty(_isAddUserFormVisible, value)
            End Set
        End Property

        Private _newUsername As String
        Public Property NewUsername As String
            Get
                Return _newUsername
            End Get
            Set(ByVal value As String)
                SetProperty(_newUsername, value)
            End Set
        End Property

        Private _newPassword As String
        Public Property NewPassword As String
            Get
                Return _newPassword
            End Get
            Set(ByVal value As String)
                SetProperty(_newPassword, value)
            End Set
        End Property

        Private _selectedRole As ComboBoxItem
        Public Property SelectedRole As ComboBoxItem
            Get
                Return _selectedRole
            End Get
            Set(ByVal value As ComboBoxItem)
                SetProperty(_selectedRole, value)
            End Set
        End Property

        Private _formErrorMessage As String
        Public Property FormErrorMessage As String
            Get
                Return _formErrorMessage
            End Get
            Set(ByVal value As String)
                SetProperty(_formErrorMessage, value)
            End Set
        End Property

        ' Commands
        Public Property ToggleAddUserFormCommand As IRelayCommand
        Public Property SaveUserCommand As IRelayCommand
        Public Property DeleteUserCommand As IRelayCommand
        Public Property RefreshCommand As IRelayCommand

        Public Sub New()
            _dbService = New DatabaseService()
            LoadUsers()
            
            ToggleAddUserFormCommand = New RelayCommand(AddressOf ToggleForm)
            SaveUserCommand = New RelayCommand(AddressOf SaveUser)
            DeleteUserCommand = New RelayCommand(AddressOf DeleteUser, Function() SelectedUser IsNot Nothing)
            RefreshCommand = New RelayCommand(AddressOf LoadUsers)
        End Sub

        Private Sub LoadUsers()
            Dim userList = _dbService.GetAllUsers()
            Users = New ObservableCollection(Of User)(userList)
        End Sub

        Private Sub ToggleForm()
            IsAddUserFormVisible = Not IsAddUserFormVisible
            FormErrorMessage = String.Empty
            If Not IsAddUserFormVisible Then
                ClearForm()
            End If
        End Sub

        Private Sub ClearForm()
            NewUsername = String.Empty
            NewPassword = String.Empty
            SelectedRole = Nothing
        End Sub

        Private Sub SaveUser()
            If String.IsNullOrWhiteSpace(NewUsername) OrElse String.IsNullOrWhiteSpace(NewPassword) OrElse SelectedRole Is Nothing Then
                FormErrorMessage = "All fields are required."
                Return
            End If

            Dim roleText = SelectedRole.Content.ToString()
            ' Default permissions based on role
            Dim permissions = If(roleText = "Admin", "{""All"": true}", "{""POS"": true}")

            If _dbService.CreateUser(NewUsername, NewPassword, roleText, permissions) Then
                LoadUsers()
                ToggleForm()
            Else
                FormErrorMessage = "Failed to create user. Username might already exist."
            End If
        End Sub

        Private Sub DeleteUser()
            If SelectedUser IsNot Nothing Then
                If SelectedUser.Role = "Admin" AndAlso Users.Where(Function(u) u.Role = "Admin").Count() <= 1 Then
                    ' Prevent deleting the last admin
                    Return
                End If

                If _dbService.DeleteUser(SelectedUser.Id) Then
                    Users.Remove(SelectedUser)
                End If
            End If
        End Sub
    End Class
End Namespace