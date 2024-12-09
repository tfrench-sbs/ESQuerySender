Imports Elastic.Clients.Elasticsearch
Imports Elastic.Clients.Elasticsearch.QueryDsl
Imports Elastic.Clients.Elasticsearch.Core.Search
Imports System.Text.RegularExpressions

Public Module ElasticSearchInterface

    Const DefaultSize As Integer = 12

    ReadOnly Braces As Regex = New Regex("[\(\)]")
    ReadOnly SpecialCharacters As Regex = New Regex("[^a-z-A-Z0-9]")
    ReadOnly Whitespace As Regex = New Regex("\s+")
    ReadOnly IMEI As Regex = New Regex("^\s*\d{2}[-\s]?\d{6}[-\s]?\d{6}[-\s]?\d\s*$")
    ReadOnly NonNumbers As Regex = New Regex("[^\d]")

    ReadOnly StandardQueryFields As Fields = {
        "type",
        "nfumdescription",
        "alttype",
        "alttype2",
        "brandname"
    }

    ReadOnly ExactQueryFields As Fields = {
        "alttype2"
    }

    ReadOnly CanonicalProductNames As IDictionary(Of String, String) = New Dictionary(Of String, String) From {
        {"galaxy watch 4", "galaxy watch4"},
        {"apple watch s(\d+)", "apple watch series $1"},
        {"garmin venu(?! sq)", "garmin venu sq"},
        {"i phone", "iphone"},
        {"mac book", "macbook"},
        {"ps(\d+)", "playstation $1"}
    }

    ReadOnly ExtractableBrands As IDictionary(Of String, String) = New Dictionary(Of String, String) From {
        {"apple", "apple"},
        {"google", "google"},
        {"samsung", "samsung"},
        {"fitbit", "fitbit"},
        {"fit bit", "fitbit"},
        {"huawei", "huawei"},
        {"panasonic", "panasonic"},
        {"lg", "lg"},
        {"sony", "sony"},
        {"hp", "hp"}
    }

    Public Function Find(
        query As String,
        Optional category As Integer = 0,
        Optional family As Integer = 0
    ) As SearchResponse(Of Product)

        query = NormaliseQuery(query)

        Dim extractedBrands = ExtractBrands(query)

        Return ExecuteSearchRequests(
            GenerateStandardRequests(
                query:=extractedBrands.CleanQuery,
                brands:=extractedBrands.Brands,
                category:=category,
                family:=family
            )
        ).Results

    End Function

    Public Function FindOrSuggest(
        query As String,
        Optional category As Integer = 0,
        Optional family As Integer = 0
    ) As (Results As SearchResponse(Of Product), FallbackResult As SearchResponse(Of Product))

        query = NormaliseQuery(query)

        query = FixMemorySpacing(query)

        Dim extractedBrands = ExtractBrands(query)

        Return ExecuteSearchRequests(
            GenerateStandardRequests(
                query:=extractedBrands.CleanQuery,
                brands:=extractedBrands.Brands,
                category:=category,
                family:=family
            ),
            Nothing
        )

        'GenerateSearchSuggestionRequest(query)


    End Function

    Private Function ExecuteSearchRequests(
        searchRequests As IEnumerable(Of SearchRequest),
        Optional fallback As SearchRequest = Nothing
    ) As (Results As SearchResponse(Of Product), FallbackResult As SearchResponse(Of Product))

        Dim client As ElasticsearchClient = InitialiseElasticSearch()
        Dim results As SearchResponse(Of Product) = Nothing
        Dim fallbackResult As SearchResponse(Of Product) = Nothing

        For Each searchRequest In searchRequests

            results = client.Search(Of Product)(searchRequest)

            If results.Documents.Any() Then
                Return (results, fallbackResult)
            End If

            If fallback IsNot Nothing And fallbackResult Is Nothing Then
                fallbackResult = client.Search(Of Product)(fallback)
            End If

        Next

        Return (results, fallbackResult)

    End Function

    Private Function InitialiseElasticSearch() As ElasticsearchClient

        Dim settings As New ElasticsearchClientSettings(New Uri("http://sbs-devess01:9200/"))

        settings.DefaultIndex("products")
        settings.DisableDirectStreaming(True)

        Return New ElasticsearchClient(settings)

    End Function

    Private Function GenerateStandardRequests(
        query As String,
        Optional brands As IEnumerable(Of String) = Nothing,
        Optional category As Integer = 0,
        Optional family As Integer = 0,
        Optional from As Integer = 0,
        Optional size As Integer = DefaultSize
    ) As IEnumerable(Of SearchRequest)

        Return New List(Of SearchRequest) From {
            GenerateRequest(
                query,
                brands:=brands,
                category:=category,
                family:=family,
                fuzziness:=Nothing,
                from:=from,
                size:=size
            )
        }

    End Function

    Private Function GenerateRequest(
        queryString As String,
        Optional brands As IEnumerable(Of String) = Nothing,
        Optional category As Integer = 0,
        Optional family As Integer = 0,
        Optional queryTransform As Func(Of String, String) = Nothing,
        Optional fuzziness As Fuzziness = Nothing,
        Optional from As Integer = 0,
        Optional size As Integer = DefaultSize,
        Optional exactMatch As Boolean = False
    ) As SearchRequest

        Dim query As Query

        If exactMatch Then
            query = SmallFieldsQuery(queryString, fuzziness)
        Else
            query = MainQuery(queryString, queryTransform, fuzziness)
            query = query Or SmallFieldsQuery(queryString, New Fuzziness("Auto"))
            query = query Or AllFieldsQueryNoAnalyzer(queryString)
        End If

        If family <> 0 Then

            query = query And FamilyQuery(family)

        ElseIf category <> 0 Then

            query = query And CategoryQuery(category)

        End If

        If brands.Any() Then

            query = query And brands.
                Select(Function(brand) BrandQuery(brand)).
                Aggregate(Function(a, b) a Or b)

        End If

        Return CompileRequest(query, from, size)

    End Function

    'Private Function GenerateSearchSuggestionRequest(query As String) As SearchRequest

    '    Return New SearchRequest(Of Product) With {
    '        .TypedKeys = False,
    '        .Suggest = GenerateSuggestContainer(query)
    '    }

    'End Function

    'Private Function GenerateSuggestContainer(query As String) As SuggestContainer

    '    Return New SuggestContainer(
    '        New Dictionary(Of String, ISuggestBucket) From {
    '            {
    '                "completion-suggest",
    '                New SuggestBucket With {
    '                    .Text = query,
    '                    .Phrase = StandardPhraseSuggester()
    '                }
    '            }
    '        }
    '    )

    'End Function

    Private Function StandardPhraseSuggester() As PhraseSuggester

        Return New PhraseSuggester With {
            .Field = "type",
            .Size = 1,
            .GramSize = 3,
            .DirectGenerator = {
                New DirectGenerator With {
                    .Field = "type",
                    .SuggestMode = SuggestMode.Always
                }
            },
            .Highlight = New PhraseSuggestHighlight With {
                .PreTag = "<em>",
                .PostTag = "</em>"
            }
        }

    End Function

    Private Function CompileRequest(query As Query, Optional from As Integer = 0, Optional size As Integer = DefaultSize)

        Dim indices As Indices = Indices.Parse("products")

        Return New SearchRequest(indices) With {
            .From = from,
            .Size = size,
            .Sort = DescendingScoreSort(),
            .Query = query
        }

    End Function

    Private Function NormaliseQuery(query As String) As String

        Dim normalisedSearchString = query.
            Trim().
            ToLower()

        normalisedSearchString = Braces.Replace(normalisedSearchString, String.Empty)
        normalisedSearchString = NormaliseIMEI(normalisedSearchString)

        Return normalisedSearchString

    End Function

    Private Function SimplifyQueryString(query As String) As String

        Return SpecialCharacters.Replace(query, String.Empty)

    End Function

    Private Function NormaliseIMEI(query As String) As String
        Return query

    End Function

    Private Function DescendingScoreSort() As IList(Of SortOptions)

        Return New List(Of SortOptions) From {SortOptions.Score(New ScoreSort With {.Order = SortOrder.Desc})}

    End Function

    Private Function MainQuery(
        queryString As String,
        Optional queryTransform As Func(Of String, String) = Nothing,
        Optional fuzziness As Fuzziness = Nothing
    ) As Query

        If queryTransform IsNot Nothing Then

            Return AllFieldsQuery(queryTransform(queryString), fuzziness) Or
                AllFieldsQuery(queryTransform(SimplifyQueryString(queryString)), fuzziness)
        Else

            Return AllFieldsQuery(queryString, fuzziness) Or AllFieldsQuery(SimplifyQueryString(queryString), fuzziness)

        End If

    End Function

    Private Function AllFieldsQueryNoAnalyzer(queryString As String, Optional fuzziness As Fuzziness = Nothing) As Query

        Return New MultiMatchQuery With {.Query = queryString, .Fields = StandardQueryFields, .Fuzziness = fuzziness}

    End Function

    Private Function AllFieldsQuery(queryString As String, Optional fuzziness As Fuzziness = Nothing) As Query

        Return New MultiMatchQuery With {.Query = queryString, .Fields = StandardQueryFields, .Fuzziness = fuzziness, .Analyzer = "custom_analyzer"}

    End Function

    Private Function SmallFieldsQuery(queryString As String, Optional fuzziness As Fuzziness = Nothing) As Query

        Return New MultiMatchQuery With {.Query = queryString, .Fields = ExactQueryFields, .Fuzziness = fuzziness, .Analyzer = "custom_analyzer"}

    End Function

    Private Function BrandQuery(brand As String) As Query

        Return New TermQuery("brandname") With {.Field = "brandname", .Value = brand}

    End Function

    Private Function CategoryQuery(category As Integer) As Query

        Return New TermQuery("categoryid") With {.Field = "categoryid", .Value = category}

    End Function

    Private Function FamilyQuery(family As Integer) As Query

        Return New TermQuery("familyid") With {.Field = "familyid", .Value = family}

    End Function

    Private Function CanonicaliseProductNames(query As String) As String

        Return SetMatchingString(query, CanonicalProductNames.Keys).
            Aggregate(query, Function(clean, [next]) [next].[regex].Replace(clean, CanonicalProductNames([next].identity))).
            Trim()

    End Function

    Private Function ExtractBrands(query As String) As ExtractedBrands

        Dim extractedBrands = SetMatchingString(query, ExtractableBrands.Keys).
            ToList()

        Return New ExtractedBrands With {
            .CleanQuery = Whitespace.Replace(
                extractedBrands.
                    Aggregate(query, Function(clean, [next]) [next].[regex].Replace(clean, String.Empty)).
                    Trim(),
                " "
            ),
            .Brands = extractedBrands.
                Select(Function(brand) ExtractableBrands(brand.identity)).
                ToList()
        }

    End Function

    Private Function FixMemorySpacing(query As String) As String

        Return Regex.Replace(query, "([0-9])?([gGtT][bB])", "$1 $2")

    End Function

    Private Function SetMatchingString(
        query As String,
        [set] As IEnumerable(Of String)
    ) As IEnumerable(Of (identity As String, [regex] As Regex))

        Return [set].
            Select(Function(identity) (identity, [regex]:=New Regex($"\b{identity}\b"))).
            Where(Function(brand) brand.[regex].IsMatch(query))

    End Function

    Private Structure ExtractedBrands

        Public CleanQuery As String
        Public Brands As IEnumerable(Of String)

    End Structure

End Module