using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class empleados
    {
        [Key]
        public int id { get; set; }
        public int codigo { get; set; }
        public string contrasena { get; set; }
        public string nombre { get; set; }
        public string apellido { get; set; }
        public string telefono { get; set; }
        public int cargo_id { get; set; }
        public int estado { get; set; }

    }
}
