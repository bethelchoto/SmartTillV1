Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports SmartTill.V2.Services
Imports SmartTill.V2.Models

Namespace ViewModels
    Public Class LoginViewModel
        Inherits ObservableObject

        Private ReadOnly _dbService As DatabaseService
        
        Private _username As String
        Public Property Username As String
            Get
                Return _username
            End Get
            Set(value As String)
                SetProperty(_username, value)
            End Set
        End Property

        Private _errorMessage As String
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        Public Property LoginCommand As IRelayCommand
        
        Public Event LoginSuccessful(user As User)

        Public Sub New()
            _dbService = New DatabaseService()
            LoginCommand = New RelayCommand(AddressOf ExecuteLogin)
        End Sub

        Private Sub ExecuteLogin()
            ErrorMessage = String.Empty
            
            ' Access password from the view (best practice for secure passwords in WPF)
            ' For now, we'll assume the view passes it or uses a helper
            ' But standard MVVM often uses a PasswordBox helper or sends the SecureString
        End Sub

        Public Sub Login(password As String)
            If String.IsNullOrWhiteSpace(Username) OrElse String.IsNullOrWhiteSpace(password) Then
                ErrorMessage = "Please enter both username and password."
                Return
            End If

            Dim user = _dbService.AuthenticateUser(Username, password)
            If user IsNot Nothing Then
                RaiseEvent LoginSuccessful(user)
            Else
                ErrorMessage = "Invalid username or password."
            End If
        End Sub
    End Class
End Namespace
