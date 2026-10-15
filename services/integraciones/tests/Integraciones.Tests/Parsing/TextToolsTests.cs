using Integraciones.Application.Parsing;

namespace Integraciones.Tests.Parsing;

public class TextToolsTests
{
    [Theory]
    [InlineData("Desarrollador .NET (Híbrido)", "desarrollador-net-hibrido")]
    [InlineData("  Técnico/a   informático/a ", "tecnico-a-informatico-a")]
    [InlineData("", "")]
    public void Slug_removes_accents_and_symbols(string text, string expected) =>
        Assert.Equal(expected, TextTools.Slug(text));

    [Fact]
    public void External_id_is_capped_at_150_characters()
    {
        var id = TextTools.ExternalIdFromText("in", "Empresa", new string('a', 300));

        Assert.Equal(150, id.Length);
        Assert.StartsWith("in-empresa-", id);
    }

    [Fact]
    public void Clean_removes_invisible_preview_characters_and_nbsp()
    {
        Assert.Equal("Hola mundo", TextTools.Clean("​ Hola   mundo ﻿"));
        Assert.Null(TextTools.Clean(" ‌ "));
    }

    [Theory]
    [InlineData("Madrid (Híbrido)", "Híbrido")]
    [InlineData("100% remoto", "Remoto")]
    [InlineData("Teletrabajo", "Remoto")]
    [InlineData("In presenza", "Presencial")]
    [InlineData("Barcelona", null)]
    public void Detects_work_mode(string text, string? expected) =>
        Assert.Equal(expected, TextTools.DetectWorkMode(text));
}
