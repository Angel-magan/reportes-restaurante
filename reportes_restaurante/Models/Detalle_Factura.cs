using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class Detalle_Factura
    {
        [Key]
        public int detalle_factura_id { get; set; }
        public int factura_id { get; set; }
        public int detalle_pedido_id { get; set; }
        public decimal subtotal { get; set; }

    }
}
