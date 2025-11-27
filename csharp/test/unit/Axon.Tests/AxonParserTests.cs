using System.Globalization;
using AwesomeAssertions;
using Xunit;

namespace Axon.Tests;

public class AxonParserTests
{
    [Fact]
    public void Parse_SingleSchema_ReturnsCorrectSchemaName()
    {
        var input = """
            `User[0](id:I)
            ~
            """;

        var (schemas, _) = AxonParser.Parse(input);

        schemas.Should().ContainSingle();
        schemas[0].Name.Should().Be("User");
    }

    [Fact]
    public void Parse_SchemaWithAllTypes_ParsesAllTypesCorrectly()
    {
        var input = """
            `AllTypes[0](stringField:S,intField:I,floatField:F,boolField:B,timestampField:T)
            ~
            """;

        var (schemas, _) = AxonParser.Parse(input);

        schemas.Should().ContainSingle();
        schemas[0].Fields.Should().HaveCount(5);
        schemas[0].Fields[0].Type.Should().Be(AxonType.String);
        schemas[0].Fields[1].Type.Should().Be(AxonType.Integer);
        schemas[0].Fields[2].Type.Should().Be(AxonType.Float);
        schemas[0].Fields[3].Type.Should().Be(AxonType.Boolean);
        schemas[0].Fields[4].Type.Should().Be(AxonType.Timestamp);
    }

    [Fact]
    public void Parse_SchemaWithNullableFields_SetsIsNullableCorrectly()
    {
        var input = """
            `Test[0](required:I,optional:I?)
            ~
            """;

        var (schemas, _) = AxonParser.Parse(input);

        schemas[0].Fields[0].IsNullable.Should().BeFalse();
        schemas[0].Fields[1].IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Parse_MultipleSchemas_ParsesAllSchemas()
    {
        var input = """
            `First[0](id:I)
            ~
            `Second[0](name:S)
            ~
            `Third[0](value:F)
            ~
            """;

        var (schemas, _) = AxonParser.Parse(input);

        schemas.Should().HaveCount(3);
        schemas[0].Name.Should().Be("First");
        schemas[1].Name.Should().Be("Second");
        schemas[2].Name.Should().Be("Third");
    }

    [Fact]
    public void Parse_SchemaWithUnknownType_ThrowsException()
    {
        var input = """
            `Test[0](field:X)
            ~
            """;

        var act = () => AxonParser.Parse(input);

        act.Should().Throw<AxonParseException>();
    }

    [Fact]
    public void Parse_DataBlock_ReturnsCorrectSchemaName()
    {
        var input = """
            `User[1](id:I)
            42
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks.Should().ContainSingle();
        dataBlocks[0].SchemaName.Should().Be("User");
    }

    [Fact]
    public void Parse_DataBlock_ParsesIntegerValue()
    {
        var input = """
            `Test[1](value:I)
            12345
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(12345L);
    }

    [Fact]
    public void Parse_DataBlock_ParsesNegativeInteger()
    {
        var input = """
            `Test[1](value:I)
            -42
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(-42L);
    }

    [Fact]
    public void Parse_DataBlock_ParsesStringValue()
    {
        var input = """
            `Test[1](value:S)
            Hello World
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("Hello World");
    }

    [Fact]
    public void Parse_DataBlock_ParsesFloatValue()
    {
        var input = """
            `Test[1](value:F)
            3.14159
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(3.14159);
    }

    [Fact]
    public void Parse_DataBlock_ParsesBooleanTrue()
    {
        var input = """
            `Test[1](value:B)
            1
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(true);
    }

    [Fact]
    public void Parse_DataBlock_ParsesBooleanFalse()
    {
        var input = """
            `Test[1](value:B)
            0
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(false);
    }

    [Fact]
    public void Parse_DataBlock_ParsesTimestamp()
    {
        var input = """
            `Test[1](value:T)
            2024-11-23T10:30:00Z
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        var expected = DateTime.Parse("2024-11-23T10:30:00Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        dataBlocks[0].Rows[0]["value"].Should().Be(expected);
    }

    [Fact]
    public void Parse_DataBlock_ParsesNullValue()
    {
        var input = """
            `Test[1](value:I?)
            _
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().BeNull();
    }

    [Fact]
    public void Parse_DataBlock_ParsesMultipleRows()
    {
        var input = """
            `User[3](id:I,name:S)
            1|Alice
            2|Bob
            3|Carol
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows.Should().HaveCount(3);
        dataBlocks[0].Rows[0]["id"].Should().Be(1L);
        dataBlocks[0].Rows[0]["name"].Should().Be("Alice");
        dataBlocks[0].Rows[1]["id"].Should().Be(2L);
        dataBlocks[0].Rows[1]["name"].Should().Be("Bob");
        dataBlocks[0].Rows[2]["id"].Should().Be(3L);
        dataBlocks[0].Rows[2]["name"].Should().Be("Carol");
    }

    [Fact]
    public void Parse_MultipleDataBlocks_ParsesAllBlocks()
    {
        var input = """
            `User[1](id:I)
            1
            ~
            `Order[1](id:I)
            100
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks.Should().HaveCount(2);
        dataBlocks[0].SchemaName.Should().Be("User");
        dataBlocks[1].SchemaName.Should().Be("Order");
    }

    [Fact]
    public void Parse_EscapedQuote_ParsesCorrectly()
    {
        var input = """
            `Test[1](value:S)
            Hello \"World\"
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("Hello \"World\"");
    }

    [Fact]
    public void Parse_EscapedBackslash_ParsesCorrectly()
    {
        var input = "`Test[1](value:S)\nC:\\\\Users\\\\Test\n~";

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("C:\\Users\\Test");
    }

    [Fact]
    public void Parse_EscapedNewline_ParsesCorrectly()
    {
        var input = """
            `Test[1](value:S)
            Line1\nLine2
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("Line1\nLine2");
    }

    [Fact]
    public void Parse_EscapedTab_ParsesCorrectly()
    {
        var input = """
            `Test[1](value:S)
            Col1\tCol2
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("Col1\tCol2");
    }

    [Fact]
    public void Parse_EscapedCarriageReturn_ParsesCorrectly()
    {
        var input = """
            `Test[1](value:S)
            Line1\rLine2
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("Line1\rLine2");
    }

    [Fact]
    public void Parse_EscapedPipe_ParsesCorrectly()
    {
        var input = """
            `Test[1](value:S)
            A\|B\|C
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("A|B|C");
    }

    [Fact]
    public void Parse_QuotedStringWithPipe_PreservesPipe()
    {
        var input = """
            `Test[1](value:S)
            "A|B|C"
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be("A|B|C");
    }

    [Fact]
    public void Parse_QuotedStringInMiddle_PreservesContent()
    {
        var input = """
            `Test[1](first:S,second:S,third:S)
            A|"B|C"|D
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["first"].Should().Be("A");
        dataBlocks[0].Rows[0]["second"].Should().Be("B|C");
        dataBlocks[0].Rows[0]["third"].Should().Be("D");
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyResults()
    {
        var (schemas, dataBlocks) = AxonParser.Parse(string.Empty);

        schemas.Should().BeEmpty();
        dataBlocks.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceOnlyInput_ReturnsEmptyResults()
    {
        var (schemas, dataBlocks) = AxonParser.Parse("   \n\n   \n   ");

        schemas.Should().BeEmpty();
        dataBlocks.Should().BeEmpty();
    }

    [Fact]
    public void Parse_DataBlockWithNoRows_ReturnsEmptyRowsList()
    {
        var input = """
            `Test[0](id:I)
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks.Should().ContainSingle();
        dataBlocks[0].Rows.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WindowsLineEndings_ParsesCorrectly()
    {
        var input = "`Test[1](id:I)\r\n42\r\n~\r\n";

        var (schemas, dataBlocks) = AxonParser.Parse(input);

        schemas.Should().ContainSingle();
        dataBlocks.Should().ContainSingle();
        dataBlocks[0].Rows[0]["id"].Should().Be(42L);
    }

    [Fact]
    public void Parse_MixedLineEndings_ParsesCorrectly()
    {
        var input = "`Test[1](id:I)\n42\r\n~";

        var (schemas, dataBlocks) = AxonParser.Parse(input);

        schemas.Should().ContainSingle();
        dataBlocks.Should().ContainSingle();
    }

    [Fact]
    public void Parse_TrailingPipe_AddsEmptyValue()
    {
        var input = """
            `Test[1](first:S,second:S)
            Hello|
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["first"].Should().Be("Hello");
        dataBlocks[0].Rows[0]["second"].Should().Be(string.Empty);
    }

    [Fact]
    public void Parse_LargeInteger_ParsesAsLong()
    {
        var input = """
            `Test[1](value:I)
            9223372036854775807
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(long.MaxValue);
    }

    [Fact]
    public void Parse_ScientificNotationFloat_ParsesCorrectly()
    {
        var input = """
            `Test[1](value:F)
            1.5E10
            ~
            """;

        var (_, dataBlocks) = AxonParser.Parse(input);

        dataBlocks[0].Rows[0]["value"].Should().Be(1.5E10);
    }

    [Fact]
    public void Parse_CompleteExample_ParsesCorrectly()
    {
        var input = """
            `User[5](id:I,name:S,email:S,active:B,age:I?,country:S?)
            1|Alice|alice@example.com|1|28|US
            2|Bob|bob@example.com|0|_|UK
            3|Carol|carol@example.com|1|35|_
            4|Dave|dave@example.com|1|42|CA
            5|Eve|eve@example.com|0|31|AU
            ~
            `Order[2](id:I,userId:I,total:F,timestamp:T)
            101|1|299.99|2024-11-23T10:30:00Z
            102|2|149.50|2024-11-23T11:15:00Z
            ~
            """;

        var (schemas, dataBlocks) = AxonParser.Parse(input);

        schemas.Should().HaveCount(2);
        schemas[0].Name.Should().Be("User");
        schemas[0].Fields.Should().HaveCount(6);
        schemas[1].Name.Should().Be("Order");
        schemas[1].Fields.Should().HaveCount(4);

        dataBlocks[0].Rows.Should().HaveCount(5);
        dataBlocks[0].Rows[0]["id"].Should().Be(1L);
        dataBlocks[0].Rows[0]["name"].Should().Be("Alice");
        dataBlocks[0].Rows[0]["active"].Should().Be(true);
        dataBlocks[0].Rows[0]["age"].Should().Be(28L);
        dataBlocks[0].Rows[1]["age"].Should().BeNull();
        dataBlocks[0].Rows[2]["country"].Should().BeNull();

        dataBlocks[1].Rows.Should().HaveCount(2);
        dataBlocks[1].Rows[0]["id"].Should().Be(101L);
        dataBlocks[1].Rows[0]["total"].Should().Be(299.99);
    }

    [Fact]
    public void Parse_UltraCompactFormat_ParsesCorrectly()
    {
        var input = """
            `Test[2]{name,value}
            Alice|123
            Bob|456
            ~
            """;

        var (schemas, dataBlocks) = AxonParser.Parse(input);

        schemas.Should().ContainSingle();
        schemas[0].Fields.Should().HaveCount(2);
        schemas[0].Fields[0].Type.Should().Be(AxonType.String);
        schemas[0].Fields[0].IsNullable.Should().BeTrue();

        dataBlocks[0].Rows.Should().HaveCount(2);
        dataBlocks[0].Rows[0]["name"].Should().Be("Alice");
        dataBlocks[0].Rows[0]["value"].Should().Be("123");
    }
}
