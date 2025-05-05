using MongoDB.Driver;
using System.Collections.Generic;

namespace UrbanFoodApp
{
    public class Review
    {
        public string Id { get; set; }
        public int ProductID { get; set; }
        public int CustomerID { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }

    public class ReviewService
    {
        private readonly IMongoCollection<Review> _reviews;

        public ReviewService(string connectionString, string databaseName, string collectionName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _reviews = database.GetCollection<Review>(collectionName);
        }

        public List<Review> GetReviewsForProduct(int productId)
        {
            return _reviews.Find(review => review.ProductID == productId).ToList();
        }

        public Review AddReview(Review review)
        {
            review.ReviewDate = DateTime.Now;
            _reviews.InsertOne(review);
            return review;
        }

        public decimal GetAverageRating(int productId)
        {
            var reviews = GetReviewsForProduct(productId);
            if (reviews.Count == 0) return 0;
            
            int total = 0;
            foreach (var review in reviews)
            {
                total += review.Rating;
            }
            
            return (decimal)total / reviews.Count;
        }
    }
}
