using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagement.Constants;

public class ErrorConsts
{
    public const string InternalServerError = "Internal Server Error";
    public const string ServerError = "An error occurred when processing your request";
    public const string StatusCheck = "Status is required.";
    public const string Invalid = "Invalid.";
    public const string Code = "Code should not exceed 5 characters";
}
