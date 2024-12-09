Public Class Product
    Public Property BestPrice As Object
    Public Property ProductId As Integer
    Public Property FamilyId As Integer
    Public Property PerformanceQuartile As Integer
    Public Property DcPavCount As Object
    Public Property Attributes As List(Of ProductAttribute)
    Public Property AltType As String
    Public Property Ean As Object
    Public Property GfkProductId As Long
    Public Property OldProductId As Object
    Public Property ItemId As Integer
    Public Property EncodexStart As DateTime
    Public Property Timestamp As DateTime
    Public Property AltType2 As String
    Public Property BrandName As String
    Public Property BrandId As Long
    Public Property AveragePrice As Double
    Public Property Score As Double
    Public Property Type As String
    Public Property ExtendedFamilyId As Object
    Public Property PriceLastUpdated As DateTime
    Public Property Colour As String
    Public Property Version As String
    Public Property PriceWhichSupplier As Integer
    Public Property Deleted As Boolean
    Public Property CnetCatId As String
    Public Property PictureUrl As String
    Public Property FamilyName As String
    Public Property InformationUrl As String
    Public Property NfumDescription As String
    Public Property PreviousProductId As Object
    Public Property CategoryName As String
    Public Property CnetId As Object
    Public Property SupplierName As String
    Public Shared Sub PrintProductDetails(product As Product)
        Console.WriteLine("Product Details:")
        Console.WriteLine($"Product ID: {product.ProductId}")
        Console.WriteLine($"Family ID: {product.FamilyId}")
        Console.WriteLine($"Performance Quartile: {product.PerformanceQuartile}")
        Console.WriteLine($"Best Price: {If(product.BestPrice Is Nothing, "N/A", product.BestPrice.ToString())}")
        Console.WriteLine($"DC Pav Count: {If(product.DcPavCount Is Nothing, "N/A", product.DcPavCount.ToString())}")
        Console.WriteLine($"Alt Type: {product.AltType}")
        Console.WriteLine($"EAN: {If(product.Ean Is Nothing, "N/A", product.Ean.ToString())}")
        Console.WriteLine($"GFK Product ID: {product.GfkProductId}")
        Console.WriteLine($"Old Product ID: {If(product.OldProductId Is Nothing, "N/A", product.OldProductId.ToString())}")
        Console.WriteLine($"Item ID: {product.ItemId}")

        Console.WriteLine($"Encodex Start: {product.EncodexStart}")
        Console.WriteLine($"Timestamp: {product.Timestamp}")
        Console.WriteLine($"Price Last Updated: {product.PriceLastUpdated}")

        Console.WriteLine($"Alt Type 2: {product.AltType2}")
        Console.WriteLine($"Brand Name: {product.BrandName}")
        Console.WriteLine($"Brand ID: {product.BrandId}")
        Console.WriteLine($"Average Price: {product.AveragePrice}")
        Console.WriteLine($"Score: {product.Score}")
        Console.WriteLine($"Type: {product.Type}")
        Console.WriteLine($"Extended Family ID: {If(product.ExtendedFamilyId Is Nothing, "N/A", product.ExtendedFamilyId.ToString())}")
        Console.WriteLine($"Colour: {product.Colour}")
        Console.WriteLine($"Version: {product.Version}")
        Console.WriteLine($"Price Which Supplier: {product.PriceWhichSupplier}")
        Console.WriteLine($"Deleted: {product.Deleted}")
        Console.WriteLine($"CNET Cat ID: {product.CnetCatId}")
        Console.WriteLine($"Picture URL: {product.PictureUrl}")
        Console.WriteLine($"Family Name: {product.FamilyName}")
        Console.WriteLine($"Information URL: {product.InformationUrl}")
        Console.WriteLine($"NFUM Description: {product.NfumDescription}")
        Console.WriteLine($"Previous Product ID: {If(product.PreviousProductId Is Nothing, "N/A", product.PreviousProductId.ToString())}")
        Console.WriteLine($"Category Name: {product.CategoryName}")
        Console.WriteLine($"CNET ID: {If(product.CnetId Is Nothing, "N/A", product.CnetId.ToString())}")
        Console.WriteLine($"Supplier Name: {product.SupplierName}")

        Console.WriteLine(vbCrLf & "Product Attributes:")
        If product.Attributes IsNot Nothing Then
            For Each attr As ProductAttribute In product.Attributes
                Console.WriteLine($"{attr.AttributeName}: {attr.Value}")
            Next
        Else
            Console.WriteLine("No attributes available")
        End If
    End Sub
End Class

Public Class ProductAttribute
    Public Property AttributeName As String
    Public Property Value As String
End Class