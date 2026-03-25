Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports SmartTill.V2.Models
Imports SmartTill.V2.Services
Imports System.Collections.ObjectModel

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
            End Set
        End Property

        Public Property AddUserCommand As IRelayCommand
        Public Property DeleteUserCommand As IRelayCommand
        Public Property RefreshCommand As IRelayCommand

        Public Sub New()
            _dbService = New DatabaseService()
            LoadUsers()
            
            AddUserCommand = New RelayCommand(AddressOf AddUser)
            DeleteUserCommand = New RelayCommand(AddressOf DeleteUser, Function() SelectedUser IsNot Nothing)
            RefreshCommand = New RelayCommand(AddressOf LoadUsers)
        End Sub

        Private Sub LoadUsers()
            Dim userList = _dbService.GetAllUsers()
            Users = New ObservableCollection(Of User)(userList)
        End Sub

        Private Sub AddUser()
            ' This will eventually open a dialog or show a form
        End Sub

        Private Sub DeleteUser()
            If SelectedUser IsNot Nothing Then
                If _dbService.DeleteUser(SelectedUser.Id) Then
                    Users.Remove(SelectedUser)
                End If
            End If
        End Sub
    End Class
End Namespace
