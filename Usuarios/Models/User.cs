using System.ComponentModel.DataAnnotations;

namespace Json_Demo.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public AddressInfo Address { get; set; } = new AddressInfo();

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Website { get; set; } = string.Empty;

        [Required]
        public CompanyInfo Company { get; set; } = new CompanyInfo();

        public class AddressInfo
        {
            [Required]
            public string Street { get; set; } = string.Empty;

            [Required]
            public string Suite { get; set; } = string.Empty;

            [Required]
            public string City { get; set; } = string.Empty;

            [Required]
            public string Zipcode { get; set; } = string.Empty;

            [Required]
            public GeoInfo Geo { get; set; } = new GeoInfo();

            public class GeoInfo
            {
                [Required]
                public string Lat { get; set; } = string.Empty;

                [Required]
                public string Lng { get; set; } = string.Empty;
            }
        }
        public class CompanyInfo
        {
            [Required]
            public string Name { get; set; } = string.Empty;

            [Required]
            public string CatchPhrase { get; set; } = string.Empty;

            [Required]
            public string Bs { get; set; } = string.Empty;
        }
    }
}