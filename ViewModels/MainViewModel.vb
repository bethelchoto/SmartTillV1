Imports CommunityToolkit.Mvvm.ComponentModel
Imports SmartTill.V2.Services

Namespace ViewModels
    Public Class MainViewModel
        Inherits ObservableObject

        Private _title As String = "SmartTill V2"
        Public Property Title As String
            Get
                Return _title
            End Get
            Set(value As String)
                SetProperty(_title, value)
            End Set
        End Property

        Private _status As String = "Checking database..."
        Public Property Status As String
            Get
                Return _status
            End Get
            Set(value As String)
                SetProperty(_status, value)
            End Set
        End Property

        Private ReadOnly _dbService As DatabaseService

        Public Sub New()
            ' Initialize commands and services here
            _dbService = New DatabaseService()
            Status = _dbService.VerifyDatabase()
        End Sub
    End Class
End Namespace
