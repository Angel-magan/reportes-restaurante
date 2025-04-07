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
        public DbSet<Detalle_Factura> Detalle_Factura { get; set; }
        public DbSet<Detalle_Pedido> Detalle_Pedido { get; set; }
        public DbSet<Pedido_Local> Pedido_Local { get; set; }
        public DbSet<mesas> mesas { get; set; }
        public DbSet<platos> platos { get; set; }
        public DbSet<combos> combos { get; set; }
        public DbSet<promociones> promociones { get; set; }


        //es una función de sql server para obtener semanas por mes 
        [DbFunction("GetIsoWeek", "dbo")]
        public static int GetIsoWeek(DateTime date) => throw new NotImplementedException();
    }
}
