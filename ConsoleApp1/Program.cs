using System;
using System.Linq;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;

namespace CompanyUserInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            string connectionString = "Server=localhost;Database=softwrDB;Integrated Security=True;TrustServerCertificate=True;";
            
            var auctionDal = new AuctionDAL(connectionString);
            var productDal = new ProductDAL(connectionString);
            var userDal = new UserDAL(connectionString);

            var currentManager = userDal.GetAll().FirstOrDefault() ?? new UserDTO { Id = Guid.NewGuid(), Name = "Default Manager" };

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                
                Console.WriteLine("Auction Control System");
                Console.WriteLine("======================");
                Console.WriteLine($"Hello, {currentManager.Name}!\n");
                Console.WriteLine("1. Show ALL Auctions");
                Console.WriteLine("2. Show ALL Products");
                Console.WriteLine("3. Create a new Auction");
                Console.WriteLine("4. Delete an Auction");
                Console.WriteLine("0. Exit");
                Console.WriteLine("----------------------");
                Console.Write("Do not concern yourself with reading this, just press a number: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ShowAuctions(auctionDal, productDal);
                        break;
                    case "2":
                        ShowProducts(productDal);
                        break;
                    case "3":
                        CreateAuction(auctionDal, productDal, currentManager);
                        break;
                    case "4":
                        DeleteAuction(auctionDal);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("\nYou will never get anything meaningful from this option.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void ShowAuctions(AuctionDAL auctionDal, ProductDAL productDal)
        {
            Console.Clear();
            Console.WriteLine("--- 📋 LIST OF ALL AUCTIONS ---\n");
            var auctions = auctionDal.GetAll();
            if (auctions.Count == 0)
            {
                Console.WriteLine("No active Auctions.");
            }
            else
            {
                foreach (var a in auctions)
                {
                    var product = productDal.GetById(a.ProductId);
                    string pName = product != null ? product.Name : "Unknown";
                    string status = a.IsActive ? "Active" : "Closed";
                    Console.WriteLine($"[{a.Id.ToString().Substring(0, 8)}] | {status} | Product: {pName} | Start: {a.StartPrice}$ | End: {a.EndDate:dd.MM.yyyy}");
                }
            }
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void ShowProducts(ProductDAL productDal)
        {
            Console.Clear();
            Console.WriteLine("--- 📦 LIST OF ALL PRODUCTS ---\n");
            var products = productDal.GetAll();
            foreach (var p in products)
            {
                Console.WriteLine($"[{p.Id.ToString().Substring(0, 8)}] {p.Name,-25} | Price: {p.Price,7}$ | Quantity: {p.Quantity}");
            }
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void CreateAuction(AuctionDAL auctionDal, ProductDAL productDal, UserDTO manager)
        {
            Console.Clear();
            Console.WriteLine("--- ➕ CREATING NEW AUCTION ---\n");
            
            var products = productDal.GetAll();
            for (int i = 0; i < products.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {products[i].Name} (Base Price: {products[i].Price}$)");
            }
            
            Console.Write("\nEnter the number of the product to sell: ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= products.Count)
            {
                var selectedProduct = products[index - 1];
                
                Console.Write("Enter the starting price (e.g. 500): ");
                if (decimal.TryParse(Console.ReadLine(), out decimal startPrice))
                {
                    Console.Write("How many days will the auction last?: ");
                    if (int.TryParse(Console.ReadLine(), out int days))
                    {
                        var newAuction = new AuctionDTO
                        {
                            ProductId = selectedProduct.Id,
                            AuctionManagerId = manager.Id,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(days),
                            StartPrice = startPrice,
                            EndPrice = 0,
                            IsActive = true
                        };

                        if (auctionDal.Add(newAuction))
                        {
                            Console.WriteLine("\nAuction successfully created and saved to the database!");
                        }
                        else
                        {
                            Console.WriteLine("\nSomething went wrong while saving to the database.\nWe don't know WHAT.");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("\nNo such kind of toy.");
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void DeleteAuction(AuctionDAL auctionDal)
        {
            Console.Clear();
            Console.WriteLine("--- ❌ DELETING AN AUCTION ---\n");
            var auctions = auctionDal.GetAll();
            for (int i = 0; i < auctions.Count; i++)
            {
                Console.WriteLine($"{i + 1}. ID: {auctions[i].Id.ToString().Substring(0,8)} | Status: {(auctions[i].IsActive ? "Active" : "Closed")}");
            }

            Console.Write("\nEnter the number of the auction to delete: ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= auctions.Count)
            {
                var selected = auctions[index - 1];
                if (auctionDal.Delete(selected.Id))
                {
                    Console.WriteLine("\nYou should never clear this type of information from the DB!\nBut you still did it.");
                }
                else
                {
                    Console.WriteLine("\nEven the DB didn't approve this action.");
                }
            }

            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }
    }
}
