namespace E_learning.Model.Courses
{
    public class LessonModel
    {
        public string LessonID { get; set; }
        public string LessonTitle { get; set; }
        public string CourseID { get; set; }

        public LessonModel() { }
        public LessonModel(string lessonID, string lessonTitle, string courseID)
        {
            LessonID = lessonID;
            LessonTitle = lessonTitle;
            CourseID = courseID;
        }
    }
}
