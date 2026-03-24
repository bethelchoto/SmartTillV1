Imports CommunityToolkit.Mvvm.ComponentModel

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

        Public Sub New()
            ' Initialize commands and services here
        End Sub
    End Class
End Namespace
