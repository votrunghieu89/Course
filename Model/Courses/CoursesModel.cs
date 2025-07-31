namespace E_learning.Model.Courses
{
    public class CoursesModel
    {
        public string CourseID { get; set; }
        public string CourseName { get; set; }
        public decimal CoursePrice { get; set; }
        public string CourseDescription { get; set; }
        public string AuthorID { get; set; }

        public CoursesModel() { }

        public CoursesModel(string courseID, string courseName, decimal coursePrice, string courseDescription, string authorID)
        {
            CourseID = courseID;
            CourseName = courseName;
            CoursePrice = coursePrice;
            CourseDescription = courseDescription;
            AuthorID = authorID;
        }
    }
}
