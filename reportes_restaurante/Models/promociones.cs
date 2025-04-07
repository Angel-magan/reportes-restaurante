using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class promociones
    {
        [Key]
        public int id { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal descuento { get; set; }
        public int estado { get; set; }
        public int categoria_id { get; set; }

    }
}
