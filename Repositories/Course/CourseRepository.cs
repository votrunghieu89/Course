using E_learning.DAL.Course;
using E_learning.Model.Courses;

namespace E_learning.Repositories.Course
{
    public class CourseRepository : ICourseRepository
    {
         
        private readonly CoursesDAL _coursesDAL;
        private readonly LessonDAL _lessonDAL;
        private readonly QuizDAL _quizDAL;
        private readonly ChoiceDAL _choiceDAL;
        private readonly QuestionDAL _questionDAL;

        public CourseRepository(
            CoursesDAL coursesDAL,
            LessonDAL lessonDAL,
            QuizDAL quizDAL,
            ChoiceDAL choiceDAL,
            QuestionDAL questionDAL)
        {
            _coursesDAL = coursesDAL;
            _lessonDAL = lessonDAL;
            _quizDAL = quizDAL;
            _choiceDAL = choiceDAL;
            _questionDAL = questionDAL;

        }
        // Course methods
        public Task<List<string>> GetAllCoursesID() => _coursesDAL.getAllCourseID();
        public Task<List<CoursesModel>> GetAllCourses(int offset, int fetchnext) => _coursesDAL.getAllCourse(offset, fetchnext);
        public Task<List<CoursesModel>> getCoursebyAuthorID(string authorID) => _coursesDAL.getCoursebyAuthorID(authorID);
        public Task<bool> InsertCourse(CoursesModel course) => _coursesDAL.InsertCourse(course);
        public Task<bool> DeleteCourse(string courseID) => _coursesDAL.deleteCourse(courseID);
       
        // Lesson methods
        public Task<List<LessonModel>> GetLessonsByCourseID(string courseID) => _lessonDAL.GetLessonByCourseID(courseID);
        public Task<bool> DeleteLessons(string lessionID) => _lessonDAL.deleteLesson(lessionID);
        public Task<bool> InsertLesson(LessonModel lesson) => _lessonDAL.insertLesson(lesson);
        public Task<List<string>> GetAllLessonsID() => _lessonDAL.GetAllLessonsID();
        public Task<string> getCourseIDbyLessonID(string lessonID) => _lessonDAL.getCourseIDbyLessonID(lessonID);
        // Quiz methods
        public Task<List<QuizModel>> GetQuizzesByCourseID(string courseID) => _quizDAL.GetQuizByCourseID(courseID);
        public Task<bool> DeleteQuiz(string quizID) => _quizDAL.DeleteQuiz(quizID);
        public Task<bool> InsertQuiz(QuizModel quiz) => _quizDAL.insertQuiz(quiz);
        public Task<List<string>> GetAllQuizID() => _quizDAL.GetAllQuizID();
        // Choice methods
        public Task<List<ChoiceModel>> GetChoicesByQuizID(string quizID) => _choiceDAL.GetChoicesByQuizID(quizID);
        public Task<bool> DeleteChoice(string choiceID) => _choiceDAL.deleteChoice(choiceID);
        public Task<bool> InsertChoice(ChoiceModel choice) => _choiceDAL.InsertChoice(choice);
        public Task<List<string>> getAllChoiceID() => _choiceDAL.getAllChoiceID();
        // Question methods
        public Task<List<QuestionModel>> GetQuestionsByQuizID(string quizID) => _questionDAL.GetQuestionsByQuizID(quizID);
        public Task<bool> DeleteQuestion(string questionID) => _questionDAL.DeleteQuestion(questionID);
        public Task<bool> InsertQuestion(QuestionModel question) => _questionDAL.InsertQuestion(question);
        public Task<List<string>> getALLQuestionID() => _questionDAL.getALLQuestionID();

     
    }
}

