Imports SmartTill.V2.ViewModels

Partial Public Class POSView
    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub Search_GotFocus(sender As Object, e As RoutedEventArgs)
        Dim vm = TryCast(DataContext, POSViewModel)
        If vm IsNot Nothing Then
            vm.IsSearchDropdownOpen = Not String.IsNullOrWhiteSpace(vm.BarcodeInput)
        End If
    End Sub
End Class