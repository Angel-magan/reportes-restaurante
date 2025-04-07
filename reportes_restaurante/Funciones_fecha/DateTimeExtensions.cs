namespace reportes_restaurante.Funciones_fecha
{
    public static class DateTimeExtensions
    {

        public static int GetIsoWeek(this DateTime date)
        {
            // Asegúrate de que la fecha está en el primer día de la semana
            var dayOfYear = date.DayOfYear;
            var startOfYear = new DateTime(date.Year, 1, 1);
            var dayOfWeek = (int)startOfYear.DayOfWeek;

            // Ajuste si el primer día del año no es lunes
            if (dayOfWeek <= 4)
            {
                startOfYear = startOfYear.AddDays(-dayOfWeek);
            }
            else
            {
                startOfYear = startOfYear.AddDays(7 - dayOfWeek);
            }

            var week = (int)((date - startOfYear).Days / 7) + 1;
            return week;
        }
    }
}
