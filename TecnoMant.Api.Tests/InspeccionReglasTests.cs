using TecnoMant.Api.Modelos;
using TecnoMant.Api.Reglas;
using Xunit;

namespace TecnoMant.Api.Tests;

/// <summary>Validaciones de la inspección y cálculo del estado general.</summary>
public class InspeccionReglasTests
{
    // Unos pocos bytes cualesquiera: las reglas no comprueban que la foto sea un JPEG real.
    private static readonly byte[] FotoDePrueba = { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03, 0xFF, 0xD9 };

    // Pedido válido base (el ejemplo del contrato). Cada prueba lo cambia en un solo punto.
    private static InspeccionPedido PedidoValido() => new()
    {
        IdOrden = 3,
        FechaHoraReal = new DateTime(2026, 10, 9, 14, 30, 0),
        Observaciones = "Equipo con ruido en puertas",
        Items = new List<ItemPedido>
        {
            new() { NombreItem = "Funcionamiento", Resultado = "Bien", Observacion = null },
            new() { NombreItem = "Ruidos", Resultado = "Malo", Observacion = "Ruido fuerte en puertas" }
        },
        Evidencias = new List<EvidenciaPedido>
        {
            new() { NombreArchivo = "foto1.jpg", FotoBase64 = Convert.ToBase64String(FotoDePrueba) }
        }
    };

    private static List<ItemPedido> ItemsCon(params string[] resultados) =>
        resultados.Select(resultado => new ItemPedido { NombreItem = "Puertas", Resultado = resultado }).ToList();

    // Valida un pedido que debe fallar y devuelve su mensaje de error.
    private static string ErrorDe(InspeccionPedido pedido)
    {
        var error = InspeccionReglas.Validar(pedido, out var fotos);

        Assert.NotNull(error);
        Assert.Empty(fotos); // con error no se entrega ninguna foto
        return error;
    }

    [Fact]
    public void CalcularEstadoGeneral_ConAlgunItemEnMalo_EsMalo()
    {
        var estado = InspeccionReglas.CalcularEstadoGeneral(ItemsCon("Bien", "Regular", "Malo"));

        Assert.Equal("Malo", estado);
    }

    [Fact]
    public void CalcularEstadoGeneral_SinMaloYConAlgunItemEnRegular_EsRegular()
    {
        var estado = InspeccionReglas.CalcularEstadoGeneral(ItemsCon("Bien", "Regular", "Bien"));

        Assert.Equal("Regular", estado);
    }

    [Fact]
    public void CalcularEstadoGeneral_ConTodosLosItemsEnBien_EsBien()
    {
        var estado = InspeccionReglas.CalcularEstadoGeneral(ItemsCon("Bien", "Bien"));

        Assert.Equal("Bien", estado);
    }

    [Fact]
    public void Validar_PedidoValido_NoDaErrorYEntregaLaFotoDecodificada()
    {
        var error = InspeccionReglas.Validar(PedidoValido(), out var fotos);

        Assert.Null(error);
        var foto = Assert.Single(fotos);
        Assert.Equal(FotoDePrueba, foto);
    }

    [Fact]
    public void Validar_SinFechaHoraReal_DaError()
    {
        var pedido = PedidoValido();
        pedido.FechaHoraReal = null;

        var error = ErrorDe(pedido);

        Assert.StartsWith("fechaHoraReal ", error);
    }

    [Fact]
    public void Validar_SinItems_DaError()
    {
        var conListaVacia = PedidoValido();
        conListaVacia.Items = new List<ItemPedido>();

        var sinLista = PedidoValido();
        sinLista.Items = null;

        Assert.StartsWith("items ", ErrorDe(conListaVacia));
        Assert.StartsWith("items ", ErrorDe(sinLista));
    }

    [Fact]
    public void Validar_NombreItemVacio_DaError()
    {
        var pedido = PedidoValido();
        pedido.Items![1].NombreItem = "   ";

        var error = ErrorDe(pedido);

        Assert.StartsWith("nombreItem ", error);
        Assert.EndsWith("(ítem 2)", error);
    }

    [Fact]
    public void Validar_ResultadoNoPermitido_DaError()
    {
        // Debe ser exactamente Bien, Regular o Malo: en minúsculas no vale.
        var pedido = PedidoValido();
        pedido.Items![0].Resultado = "bien";

        var error = ErrorDe(pedido);

        Assert.Equal("resultado debe ser Bien, Regular o Malo (ítem 1)", error);
    }

    [Fact]
    public void Validar_ItemMaloSinObservacion_DaError()
    {
        var pedido = PedidoValido();
        pedido.Items![1].Observacion = null;

        var error = ErrorDe(pedido);

        Assert.StartsWith("observacion ", error);
        Assert.EndsWith("(ítem 2)", error);
    }

    [Fact]
    public void Validar_ItemMaloSinFotos_DaError()
    {
        var pedido = PedidoValido();
        pedido.Evidencias = new List<EvidenciaPedido>();

        var error = ErrorDe(pedido);

        Assert.StartsWith("evidencias debe traer al menos 1 foto", error);
    }

    [Fact]
    public void Validar_MasDeCincoFotos_DaError()
    {
        var pedido = PedidoValido();
        pedido.Evidencias = Enumerable.Range(1, 6)
            .Select(n => new EvidenciaPedido { NombreArchivo = $"foto{n}.jpg", FotoBase64 = Convert.ToBase64String(FotoDePrueba) })
            .ToList();

        var error = ErrorDe(pedido);

        Assert.Equal("evidencias admite como máximo 5 fotos", error);
    }

    [Fact]
    public void Validar_FotoQueNoEsBase64Valido_DaError()
    {
        var conTextoInvalido = PedidoValido();
        conTextoInvalido.Evidencias![0].FotoBase64 = "esto no es Base64 ###";

        var conFotoVacia = PedidoValido();
        conFotoVacia.Evidencias![0].FotoBase64 = "";

        var errorDeTextoInvalido = ErrorDe(conTextoInvalido);
        var errorDeFotoVacia = ErrorDe(conFotoVacia);

        Assert.StartsWith("fotoBase64 debe ser Base64 válido", errorDeTextoInvalido);
        Assert.EndsWith("(foto 1)", errorDeTextoInvalido);
        Assert.Equal("fotoBase64 es obligatoria (foto 1)", errorDeFotoVacia);
    }

    [Fact]
    public void Validar_FotoDeMasDeDosMb_DaError()
    {
        // Justo en el límite (2 MB) la foto se acepta.
        var enElLimite = PedidoValido();
        enElLimite.Evidencias![0].FotoBase64 = Convert.ToBase64String(new byte[2 * 1024 * 1024]);

        // Con un byte más, ya no.
        var conUnByteMas = PedidoValido();
        conUnByteMas.Evidencias![0].FotoBase64 = Convert.ToBase64String(new byte[2 * 1024 * 1024 + 1]);

        // Una foto mucho mayor se descarta por la longitud del texto, sin decodificarla.
        var muyGrande = PedidoValido();
        muyGrande.Evidencias![0].FotoBase64 = Convert.ToBase64String(new byte[3 * 1024 * 1024]);

        Assert.Null(InspeccionReglas.Validar(enElLimite, out _));
        Assert.Equal("fotoBase64 supera el máximo de 2 MB (foto 1)", ErrorDe(conUnByteMas));
        Assert.Equal("fotoBase64 supera el máximo de 2 MB (foto 1)", ErrorDe(muyGrande));
    }

    [Fact]
    public void Validar_TextoMasLargoQueSuColumna_DaError()
    {
        var conObservacionesLargas = PedidoValido();
        conObservacionesLargas.Observaciones = new string('a', 1001);

        var conNombreItemLargo = PedidoValido();
        conNombreItemLargo.Items![0].NombreItem = new string('a', 121);

        var conObservacionLarga = PedidoValido();
        conObservacionLarga.Items![1].Observacion = new string('a', 501);

        Assert.Equal("observaciones admite como máximo 1000 caracteres", ErrorDe(conObservacionesLargas));
        Assert.Equal("nombreItem admite como máximo 120 caracteres (ítem 1)", ErrorDe(conNombreItemLargo));
        Assert.Equal("observacion admite como máximo 500 caracteres (ítem 2)", ErrorDe(conObservacionLarga));
    }
}
