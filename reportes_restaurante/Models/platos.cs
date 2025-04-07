using System.ComponentModel.DataAnnotations;

namespace reportes_restaurante.Models
{
    public class platos
    {
        [Key]
        public int id { get; set; }  // La clave primaria 'id'
        public string nombre { get; set; }  // Nombre del plato
        public string descripcion { get; set; }  // Descripción del plato
        public decimal precio { get; set; }  // Precio del plato
        public string imagen { get; set; }  // Ruta de la imagen (opcional)
        public int categoria_id { get; set; }  // FK a la categoría
        public int estado { get; set; }
    }
}
