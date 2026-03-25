Imports System.Windows.Data
Imports System.Windows

Namespace Views
    Public Class NullToVisibilityConverter
        Implements IValueConverter

        Public Shared Property Instance As New NullToVisibilityConverter()

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Dim inverse As Boolean = parameter?.ToString() = "Inverse"
            Dim isNull As Boolean = (value Is Nothing)

            If inverse Then
                Return If(isNull, Visibility.Visible, Visibility.Collapsed)
            Else
                Return If(isNull, Visibility.Collapsed, Visibility.Visible)
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
