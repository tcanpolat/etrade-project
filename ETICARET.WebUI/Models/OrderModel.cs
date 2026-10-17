namespace ETICARET.WebUI.Models
{
    public class OrderModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string? CartName { get; set; }
        public string? CartNumber { get; set; }
        public string? ExprationMonth { get; set; }
        public string? ExprationYear { get; set; }
        public string? CVV { get; set; }
        public string OrderNote { get; set; }
        public CartModel CartModel { get; set; }
    }
}
