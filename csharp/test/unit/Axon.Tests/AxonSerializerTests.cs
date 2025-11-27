using AwesomeAssertions;
using Xunit;

namespace Axon.Tests;

public class AxonSerializerTests
{
    [Fact]
    public void Deserialize_SimpleObject_ReturnsCorrectValues()
    {
        var axon = """
            `User[2](Id:I,Name:S,Active:B)
            1|Alice|1
            2|Bob|0
            ~
            """;

        var users = AxonSerializer.Deserialize<TestUser>(axon);

        users.Should().HaveCount(2);
        users[0].Id.Should().Be(1);
        users[0].Name.Should().Be("Alice");
        users[0].Active.Should().BeTrue();
        users[1].Id.Should().Be(2);
        users[1].Name.Should().Be("Bob");
        users[1].Active.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_WithBlockName_ReturnsOnlyMatchingBlock()
    {
        var axon = """
            `User[1](Id:I,Name:S)
            1|Alice
            ~
            `Order[1](Id:I,Total:F)
            100|99.99
            ~
            """;

        var users = AxonSerializer.Deserialize<TestUser>(axon, "User");

        users.Should().ContainSingle();
        users[0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Deserialize_WithCustomMapper_AppliesMapperCorrectly()
    {
        var axon = """
            `Data[2](Id:I,Value:S)
            1|Hello
            2|World
            ~
            """;

        var results = AxonSerializer.Deserialize(axon, row => $"{row["Id"]}: {row["Value"]}");

        results.Should().HaveCount(2);
        results[0].Should().Be("1: Hello");
        results[1].Should().Be("2: World");
    }

    [Fact]
    public void DeserializeDynamic_ReturnsDictionaries()
    {
        var axon = """
            `Test[2](Id:I,Name:S)
            1|Alice
            2|Bob
            ~
            """;

        var results = AxonSerializer.DeserializeDynamic(axon);

        results.Should().HaveCount(2);
        results[0]["Id"].Should().Be(1L);
        results[0]["Name"].Should().Be("Alice");
        results[1]["Id"].Should().Be(2L);
        results[1]["Name"].Should().Be("Bob");
    }

    [Fact]
    public void DeserializeOne_ReturnsSingleObject()
    {
        var axon = """
            `User[3](Id:I,Name:S)
            1|Alice
            2|Bob
            3|Carol
            ~
            """;

        var user = AxonSerializer.DeserializeOne<TestUser>(axon);

        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
        user.Name.Should().Be("Alice");
    }

    [Fact]
    public void DeserializeOne_EmptyData_ReturnsNull()
    {
        var axon = """
            `User[0](Id:I,Name:S)
            ~
            """;

        var user = AxonSerializer.DeserializeOne<TestUser>(axon);

        user.Should().BeNull();
    }

    [Fact]
    public void Deserialize_NullValues_HandledCorrectly()
    {
        var axon = """
            `User[2](Id:I,Name:S,Age:I?)
            1|Alice|30
            2|Bob|_
            ~
            """;

        var users = AxonSerializer.Deserialize<TestUserWithNullable>(axon);

        users.Should().HaveCount(2);
        users[0].Age.Should().Be(30);
        users[1].Age.Should().BeNull();
    }

    [Fact]
    public void Deserialize_IntegerConversion_HandlesLongToInt()
    {
        var axon = """
            `Data[1](Id:I,Count:I)
            42|100
            ~
            """;

        var results = AxonSerializer.Deserialize<TestWithInt>(axon);

        results.Should().ContainSingle();
        results[0].Id.Should().Be(42);
        results[0].Count.Should().Be(100);
    }

    [Fact]
    public void Serialize_ThenDeserialize_RoundTripsCorrectly()
    {
        var original = new List<TestUser>
        {
            new() { Id = 1, Name = "Alice", Active = true },
            new() { Id = 2, Name = "Bob", Active = false },
        };

        var axon = AxonSerializer.Serialize(original, "User");
        var restored = AxonSerializer.Deserialize<TestUser>(axon);

        restored.Should().HaveCount(2);
        restored[0].Id.Should().Be(original[0].Id);
        restored[0].Name.Should().Be(original[0].Name);
        restored[0].Active.Should().Be(original[0].Active);
        restored[1].Id.Should().Be(original[1].Id);
        restored[1].Name.Should().Be(original[1].Name);
        restored[1].Active.Should().Be(original[1].Active);
    }

    [Fact]
    public void Deserialize_CaseInsensitiveBlockName()
    {
        var axon = """
            `User[1](Id:I,Name:S)
            1|Alice
            ~
            """;

        var users = AxonSerializer.Deserialize<TestUser>(axon, "USER");

        users.Should().ContainSingle();
        users[0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Deserialize_NonExistentBlockName_ReturnsEmpty()
    {
        var axon = """
            `User[1](Id:I,Name:S)
            1|Alice
            ~
            """;

        var results = AxonSerializer.Deserialize<TestUser>(axon, "NonExistent");

        results.Should().BeEmpty();
    }

    public class TestUser
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public bool Active { get; set; }
    }

    public class TestUserWithNullable
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int? Age { get; set; }
    }

    public class TestWithInt
    {
        public int Id { get; set; }

        public int Count { get; set; }
    }
}
