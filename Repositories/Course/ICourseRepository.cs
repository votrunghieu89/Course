using E_learning.Model.Courses;

namespace E_learning.Repositories.Course
{
    public interface ICourseRepository
    {
       
            // Courses
            Task<List<string>> GetAllCoursesID();
            Task<List<CoursesModel>> GetAllCourses(int offset, int fetchnext);
            Task<List<CoursesModel>> getCoursebyAuthorID(string authorID);
            Task<bool> InsertCourse(CoursesModel course);
            Task<bool> DeleteCourse(string courseID);
       

        // Lessons
            Task<List<LessonModel>> GetLessonsByCourseID(string courseID);
            Task<bool> DeleteLessons(string courseID);
            Task<bool> InsertLesson(LessonModel lesson);
            Task<List<string>> GetAllLessonsID();
        Task<string> getCourseIDbyLessonID(string lessonID);

        // Quizzes
        Task<List<QuizModel>> GetQuizzesByCourseID(string courseID);
            Task<bool> DeleteQuiz(string quizID);
            Task<bool> InsertQuiz(QuizModel quiz);
            Task<List<string>> GetAllQuizID();
        // Choices
        Task<List<ChoiceModel>> GetChoicesByQuizID(string quizID);
            Task<bool> DeleteChoice(string choiceID);
            Task<bool> InsertChoice(ChoiceModel choice);
            Task<List<string>> getAllChoiceID();
        // Questions
        Task<List<string>> getALLQuestionID();
        Task<bool> InsertQuestion(QuestionModel question);
        Task<bool> DeleteQuestion(string questionID);
        Task<List<QuestionModel>> GetQuestionsByQuizID(string quizID);

    }
}
