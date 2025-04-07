using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class Detalle_Pedido
    {
        [Key]

        public int id_detalle_pedido { get; set; }
        public int? encabezado_id { get; set; }

        public string tipo_venta { get; set; }
        public string tipo_Item { get; set; }

        public int? item_id { get; set; }
        public int? cantidad { get; set; }

        public string comentarios { get; set; }
        public string estado { get; set; }

        public decimal? subtotal { get; set; }


    }
}
