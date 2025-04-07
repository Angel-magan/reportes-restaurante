using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class combos
    {
        [Key]
        public int id { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal precio { get; set; }
        public int categoria_id { get; set; }
        public int estado { get; set; }
    }

}

