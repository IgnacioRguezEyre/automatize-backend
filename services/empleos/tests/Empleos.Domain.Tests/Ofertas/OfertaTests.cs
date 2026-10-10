using Empleos.Domain.Ofertas;

namespace Empleos.Domain.Tests.Ofertas;

public class OfertaTests
{
    private static readonly DateOnly Llegada = new(2026, 10, 1);
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    private static Oferta NuevaOferta() =>
        Oferta.Crear("Backend Developer (.NET)", "ALTECH", "LinkedIn", Llegada);

    [Fact]
    public void Crear_deja_la_oferta_como_nueva_y_sin_fecha_de_estado()
    {
        var oferta = NuevaOferta();

        Assert.Equal(EstadoOferta.Nueva, oferta.Estado);
        Assert.Null(oferta.FechaEstado);
        Assert.Equal(Llegada, oferta.FechaLlegada);
        Assert.NotEqual(Guid.Empty, oferta.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_sin_titulo_falla(string titulo)
    {
        Assert.Throws<ArgumentException>(() => Oferta.Crear(titulo, "ALTECH", "LinkedIn", Llegada));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_sin_portal_falla(string portal)
    {
        Assert.Throws<ArgumentException>(() => Oferta.Crear("Backend Developer", "ALTECH", portal, Llegada));
    }

    [Fact]
    public void Crear_admite_empresa_sin_indicar()
    {
        var oferta = Oferta.Crear("Backend Developer", null, "Indeed", Llegada);

        Assert.Equal(string.Empty, oferta.Empresa);
    }

    [Fact]
    public void MarcarVista_desde_nueva_pasa_a_vista_con_la_fecha()
    {
        var oferta = NuevaOferta();

        oferta.MarcarVista(Hoy);

        Assert.Equal(EstadoOferta.Vista, oferta.Estado);
        Assert.Equal(Hoy, oferta.FechaEstado);
    }

    [Fact]
    public void MarcarVista_no_cambia_una_oferta_aplicada()
    {
        var oferta = NuevaOferta();
        oferta.MarcarAplicada(Hoy);

        oferta.MarcarVista(Hoy.AddDays(1));

        Assert.Equal(EstadoOferta.Aplicada, oferta.Estado);
        Assert.Equal(Hoy, oferta.FechaEstado);
    }

    [Fact]
    public void MarcarAplicada_vale_desde_nueva_y_desde_vista()
    {
        var desdeNueva = NuevaOferta();
        var desdeVista = NuevaOferta();
        desdeVista.MarcarVista(Hoy);

        desdeNueva.MarcarAplicada(Hoy);
        desdeVista.MarcarAplicada(Hoy);

        Assert.Equal(EstadoOferta.Aplicada, desdeNueva.Estado);
        Assert.Equal(EstadoOferta.Aplicada, desdeVista.Estado);
    }

    [Fact]
    public void MarcarAplicada_dos_veces_conserva_la_primera_fecha()
    {
        var oferta = NuevaOferta();
        oferta.MarcarAplicada(Hoy);

        oferta.MarcarAplicada(Hoy.AddDays(3));

        Assert.Equal(Hoy, oferta.FechaEstado);
    }

    [Fact]
    public void MarcarAplicada_en_una_descartada_falla()
    {
        var oferta = NuevaOferta();
        oferta.Descartar(Hoy);

        Assert.Throws<InvalidOperationException>(() => oferta.MarcarAplicada(Hoy));
        Assert.Equal(EstadoOferta.Descartada, oferta.Estado);
    }

    [Fact]
    public void DesmarcarAplicada_vuelve_a_vista_nunca_a_nueva()
    {
        var oferta = NuevaOferta();
        oferta.MarcarAplicada(Hoy);

        oferta.DesmarcarAplicada(Hoy.AddDays(1));

        Assert.Equal(EstadoOferta.Vista, oferta.Estado);
        Assert.Equal(Hoy.AddDays(1), oferta.FechaEstado);
    }

    [Fact]
    public void DesmarcarAplicada_no_hace_nada_si_no_estaba_aplicada()
    {
        var oferta = NuevaOferta();

        oferta.DesmarcarAplicada(Hoy);

        Assert.Equal(EstadoOferta.Nueva, oferta.Estado);
        Assert.Null(oferta.FechaEstado);
    }

    [Theory]
    [InlineData(EstadoOferta.Nueva)]
    [InlineData(EstadoOferta.Vista)]
    [InlineData(EstadoOferta.Aplicada)]
    public void Descartar_vale_desde_cualquier_estado(EstadoOferta inicial)
    {
        var oferta = NuevaOferta();
        if (inicial == EstadoOferta.Vista) oferta.MarcarVista(Hoy);
        if (inicial == EstadoOferta.Aplicada) oferta.MarcarAplicada(Hoy);

        oferta.Descartar(Hoy.AddDays(1));

        Assert.Equal(EstadoOferta.Descartada, oferta.Estado);
        Assert.Equal(Hoy.AddDays(1), oferta.FechaEstado);
    }

    [Fact]
    public void Recuperar_devuelve_una_descartada_a_vista()
    {
        var oferta = NuevaOferta();
        oferta.Descartar(Hoy);

        oferta.Recuperar(Hoy.AddDays(1));

        Assert.Equal(EstadoOferta.Vista, oferta.Estado);
    }

    [Fact]
    public void Recuperar_no_hace_nada_si_no_estaba_descartada()
    {
        var oferta = NuevaOferta();

        oferta.Recuperar(Hoy);

        Assert.Equal(EstadoOferta.Nueva, oferta.Estado);
    }

    [Theory]
    [InlineData(13, false)]
    [InlineData(14, true)]
    [InlineData(30, true)]
    public void EsAntigua_a_partir_de_los_14_dias_si_sigue_pendiente(int dias, bool esperado)
    {
        var oferta = NuevaOferta();
        oferta.MarcarVista(Llegada);

        Assert.Equal(esperado, oferta.EsAntigua(Llegada.AddDays(dias)));
    }

    [Fact]
    public void EsAntigua_es_falso_si_ya_esta_aplicada_o_descartada()
    {
        var aplicada = NuevaOferta();
        aplicada.MarcarAplicada(Llegada);
        var descartada = NuevaOferta();
        descartada.Descartar(Llegada);

        Assert.False(aplicada.EsAntigua(Llegada.AddDays(60)));
        Assert.False(descartada.EsAntigua(Llegada.AddDays(60)));
    }
}
