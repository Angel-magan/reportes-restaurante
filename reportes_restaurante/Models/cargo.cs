using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class cargo
    {
        [Key]
        public int id { get; set; }
        public string nombre { get; set; }
        public int estado { get; set; }
    }
}
