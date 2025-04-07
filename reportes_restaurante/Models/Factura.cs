using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class Factura
    {
        [Key]
        public int factura_id { get; set; }
        public string cliente_nombre { get; set; }
        public string codigo_factura { get; set; }
        public int id_pedido { get; set; }
        public string tipo_venta { get; set; }
        public int empleado_id { get; set; }
        public decimal total { get; set; }
        public DateTime fecha { get; set; }
    }
}
