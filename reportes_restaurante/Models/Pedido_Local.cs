using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class Pedido_Local
    {
        [Key]
        public int id_pedido { get; set; }
        public int id_mesa { get; set; }
        public int id_mesero { get; set; }
        public string nombre_cliente { get; set; }
        public DateTime fechaApertura { get; set; }
        public string estado { get; set; }
    }
}
