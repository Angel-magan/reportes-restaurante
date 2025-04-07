using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class mesas
    {
        [Key]
        public int id { get; set; }
        public int numero { get; set; }
        public int cantidad { get; set; }
        public string disponibilidad { get; set; }
        public int estado { get; set; }

    }
}
