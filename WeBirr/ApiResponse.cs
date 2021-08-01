namespace WeBirr
{
    public class ApiResponse<T> where T : class
    {
        public string error { get; set; } = null;
        public string errorCode { get; set; } = null;
        public T res { get; set; } = null;

    }

}
