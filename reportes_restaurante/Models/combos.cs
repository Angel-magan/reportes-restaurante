using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class combos
    {
        [Key]
        public int id { get; set; }  // La clave primaria 'id'
        public string nombre { get; set; }  // Nombre del combo
        public string descripcion { get; set; }  // Descripción del combo
        public decimal precio { get; set; }  // Precio del combo
        public int categoria_id { get; set; }  // FK a la categoría
        public int estado { get; set; }
    }
}
