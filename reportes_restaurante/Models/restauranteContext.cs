using Microsoft.EntityFrameworkCore;

namespace reportes_restaurante.Models
{
    public class restauranteContext : DbContext
    {
        public restauranteContext(DbContextOptions<restauranteContext> options) : base(options)
        {
        }

        public DbSet<Factura> Factura { get; set; }
        public DbSet<empleados> empleados { get; set; }
        public DbSet<cargo> cargo { get; set; }
        public DbSet<Pedido_Local> Pedido_Local { get; set; }

        public DbSet<Detalle_Pedido> Detalle_Pedido { get; set; }

        //es una función de sql server para obtener semanas por mes 
        [DbFunction("GetIsoWeek", "dbo")]
        public static int GetIsoWeek(DateTime date) => throw new NotImplementedException();
    }
}
