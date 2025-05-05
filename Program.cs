using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace UrbanFoodApp
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class Farmer
    {
        public int FarmerID { get; set; }
        public string FarmName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Certification { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public int FarmerID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int StockQuantity { get; set; }
        public DateTime DateAdded { get; set; }
        public string FarmName { get; set; }
        public string ContactPerson { get; set; }
    }

    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductName { get; set; }
    }

    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int AddProduct(Product product)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (var command = new OracleCommand("AddProduct", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    command.Parameters.Add("p_FarmerID", OracleDbType.Int32).Value = product.FarmerID;
                    command.Parameters.Add("p_Name", OracleDbType.Varchar2).Value = product.Name;
                    command.Parameters.Add("p_Category", OracleDbType.Varchar2).Value = product.Category;
                    command.Parameters.Add("p_Price", OracleDbType.Decimal).Value = product.Price;
                    command.Parameters.Add("p_Description", OracleDbType.Varchar2).Value = product.Description;
                    command.Parameters.Add("p_StockQuantity", OracleDbType.Int32).Value = product.StockQuantity;
                    
                    var productIdParam = new OracleParameter("p_ProductID", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(productIdParam);
                    
                    command.ExecuteNonQuery();
                    
                    return Convert.ToInt32(productIdParam.Value.ToString());
                }
            }
        }

        public List<Product> GetProductsByCategory(string category)
        {
            var products = new List<Product>();
            
            using (var connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (var command = new OracleCommand("GetProductsByCategory", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    command.Parameters.Add("p_Category", OracleDbType.Varchar2).Value = category;
                    
                    var resultParam = new OracleParameter("p_Results", OracleDbType.RefCursor)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(resultParam);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductID = Convert.ToInt32(reader["ProductID"]),
                                FarmerID = Convert.ToInt32(reader["FarmerID"]),
                                Name = reader["Name"].ToString(),
                                Category = reader["Category"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Description = reader["Description"].ToString(),
                                StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                                DateAdded = Convert.ToDateTime(reader["DateAdded"]),
                                FarmName = reader["FarmName"].ToString(),
                                ContactPerson = reader["ContactPerson"].ToString()
                            });
                        }
                    }
                }
            }
            
            return products;
        }

        public Order PlaceOrder(int customerId, Dictionary<int, int> productQuantities)
        {
            var order = new Order();
            
            using (var connection = new OracleConnection(_connectionString))
            {
                connection.Open();
                using (var command = new OracleCommand("PlaceOrder", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    command.Parameters.Add("p_CustomerID", OracleDbType.Int32).Value = customerId;
                    
                    // Convert product IDs and quantities to comma-separated strings
                    var productIds = string.Join(",", productQuantities.Keys);
                    var quantities = string.Join(",", productQuantities.Values);
                    
                    command.Parameters.Add("p_ProductIDs", OracleDbType.Varchar2).Value = productIds;
                    command.Parameters.Add("p_Quantities", OracleDbType.Varchar2).Value = quantities;
                    
                    var orderIdParam = new OracleParameter("p_OrderID", OracleDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(orderIdParam);
                    
                    var totalAmountParam = new OracleParameter("p_TotalAmount", OracleDbType.Decimal)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(totalAmountParam);
                    
                    command.ExecuteNonQuery();
                    
                    order.OrderID = Convert.ToInt32(orderIdParam.Value.ToString());
                    order.CustomerID = customerId;
                    order.TotalAmount = Convert.ToDecimal(totalAmountParam.Value.ToString());
                    order.OrderDate = DateTime.Now;
                    order.Status = "Pending";
                    
                    // Get order items (simplified - in a real app you'd call another stored procedure)
                    foreach (var item in productQuantities)
                    {
                        order.Items.Add(new OrderItem
                        {
                            ProductID = item.Key,
                            Quantity = item.Value,
                            UnitPrice = GetProductPrice(item.Key) // You'd implement this method
                        });
                    }
                }
            }
            
            return order;
        }
        
        // Additional methods would be implemented similarly
        // GetProductPrice, GetCustomer, AddCustomer, etc.
    }

    class Program
    {
        private static DatabaseManager _dbManager;
        
        static void Main(string[] args)
        {
            // Configure your Oracle connection string
            string connectionString = "User Id=your_username;Password=your_password;Data Source=your_tnsname;";
            
            _dbManager = new DatabaseManager(connectionString);
            
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                DisplayMainMenu();
                
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        ProductManagementMenu();
                        break;
                    case "2":
                        CustomerManagementMenu();
                        break;
                    case "3":
                        OrderManagementMenu();
                        break;
                    case "4":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void DisplayMainMenu()
        {
            Console.WriteLine("=== UrbanFood Management System ===");
            Console.WriteLine("1. Product Management");
            Console.WriteLine("2. Customer Management");
            Console.WriteLine("3. Order Management");
            Console.WriteLine("4. Exit");
            Console.Write("Enter your choice: ");
        }

        static void ProductManagementMenu()
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== Product Management ===");
                Console.WriteLine("1. Add New Product");
                Console.WriteLine("2. View Products by Category");
                Console.WriteLine("3. Back to Main Menu");
                Console.Write("Enter your choice: ");
                
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        AddNewProduct();
                        break;
                    case "2":
                        ViewProductsByCategory();
                        break;
                    case "3":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void AddNewProduct()
        {
            Console.Clear();
            Console.WriteLine("=== Add New Product ===");
            
            var product = new Product();
            
            Console.Write("Farmer ID: ");
            product.FarmerID = int.Parse(Console.ReadLine());
            
            Console.Write("Product Name: ");
            product.Name = Console.ReadLine();
            
            Console.Write("Category: ");
            product.Category = Console.ReadLine();
            
            Console.Write("Price: ");
            product.Price = decimal.Parse(Console.ReadLine());
            
            Console.Write("Description: ");
            product.Description = Console.ReadLine();
            
            Console.Write("Stock Quantity: ");
            product.StockQuantity = int.Parse(Console.ReadLine());
            
            try
            {
                int productId = _dbManager.AddProduct(product);
                Console.WriteLine($"\nProduct added successfully! ID: {productId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError adding product: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ViewProductsByCategory()
        {
            Console.Clear();
            Console.WriteLine("=== View Products by Category ===");
            Console.Write("Enter category: ");
            string category = Console.ReadLine();
            
            try
            {
                var products = _dbManager.GetProductsByCategory(category);
                
                Console.WriteLine($"\n=== Products in {category} ===");
                Console.WriteLine($"{"ID",-5} {"Name",-20} {"Price",-10} {"Stock",-10} {"Farm",-20}");
                Console.WriteLine(new string('-', 70));
                
                foreach (var product in products)
                {
                    Console.WriteLine($"{product.ProductID,-5} {product.Name,-20} {product.Price,-10:C} {product.StockQuantity,-10} {product.FarmName,-20}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError retrieving products: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        // Similar methods would be implemented for CustomerManagementMenu and OrderManagementMenu
        static void CustomerManagementMenu()
        {
            // Implementation for customer management
        }

        static void OrderManagementMenu()
        {
            // Implementation for order management
        }
    }
}
