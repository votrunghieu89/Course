using E_learning.DTO.Payment;
using E_learning.Model.Payment;
using E_learning.Repositories.Payment;
using E_learning.Services;
using E_learning.Services.VNPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_learning.Controllers.Payment
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly VnPayService _vnPayService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentController> _logger;
        private readonly GenerateID _generateID;
        private readonly CheckExsistingID _checkExsistingID;
        public PaymentController(VnPayService vnPayService, ILogger<PaymentController> logger, GenerateID generateID, CheckExsistingID checkExsistingID, IPaymentRepository paymentRepository)
        {
            _vnPayService = vnPayService;
            _logger = logger;
            _generateID = generateID;
            _checkExsistingID = checkExsistingID;
            _paymentRepository = paymentRepository;
        }
        [Authorize(Roles = "Admin,Student,Lecturer")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] PaymentRequestDTO model)
        {
            if (model == null || model.Amount <= 0)
                return BadRequest("Invalid payment information.");

            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(model, HttpContext, model.courseId, model.BuyerID);

                return Ok(new
                {
                    success = true,
                    paymentUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [Authorize(Roles = "Admin,Student,Lecturer")]
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VNPayReturn([FromQuery] string courseId, [FromQuery] string buyerId)
        {
            var response = _vnPayService.PaymentExecute(HttpContext.Request.Query);
            Console.WriteLine("Response: " + response.OrderInfo);
            string newPaymentID = await _checkExsistingID.GenerateUniqueIDForStringList(
                _paymentRepository.getAllPaymentIDAsync,
                _generateID.GeneratePaymentID
            );

            string[] name_desc_amout = response.OrderInfo.Split('|');
            string[] desc_amout = name_desc_amout[1].Split('-');
            string buyerName = name_desc_amout[0];
            string orderDescription = desc_amout[0];
            decimal orderAmount = decimal.Parse(desc_amout[1]);
            Console.WriteLine("Buyer Name: " + buyerName);
            Console.WriteLine("Order Description: " + orderDescription);
            Console.WriteLine("Order Amount: " + orderAmount);
            string courseID = courseId;
            Console.WriteLine("Course ID: " + response.OrderInfo);
            if (response.Success)
            {
                PaymentModel paymentModel = new PaymentModel(
                    newPaymentID,
                    buyerName,
                    orderDescription,
                    orderAmount,
                    courseID,
                    buyerId
                );
                bool isSaved = await _paymentRepository.SavePaymentAsync(paymentModel);
                if (!isSaved)
                {
                    _logger.LogError("Failed to save payment information for course ID: {CourseID}", courseID);
                    return StatusCode(500, "Failed to save payment information.");
                }
                return Ok(new
                {
                    success = true,
                    message = "Thanh toán thành công",
                    Object = new
                    {
                        PaymentID = newPaymentID,
                        BuyerName = buyerName,
                        OrderDescription = orderDescription,
                        OrderAmount = orderAmount,
                        CourseID = courseID,
                        BuyerID = buyerId
                    }
                });

            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Thanh toán thất bại",

                });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("getAllPayment")]
        [ProducesResponseType(typeof(IEnumerable<PaymentModel>), statusCode: 200)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllPayment()
        {
            try
            {
                var payments = await _paymentRepository.getAllPaymentAsync();
                if (payments == null || !payments.Any())
                {
                    return NotFound("No payments found");
                }
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all payments");
                return StatusCode(500, "Internal server error");
            }

        }
    }
}
    