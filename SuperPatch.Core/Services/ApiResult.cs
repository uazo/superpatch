using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.Services
{
  public class ApiResult<T> : ApiResult
  {
    public T Result { get; protected set; }

    public new ApiResult<T> WithSecurityException()
    {
      ApiResultUtil.WithSecurityException(this);
      return this;
    }

    public ApiResult<T> WithResult(T result)
    {
      Result = result;
      return this;
    }

    public ApiResult<T> WithOk()
    {
      ApiResultUtil.WithOk(this);
      return this;
    }

    public new ApiResult<T> WithMessage(string message)
    {
      this.Message = message;
      return this;
    }
  }

  public class ApiResult
  {
    public bool IsOk { get; set; } = false;
    public string Message { get; internal protected set; }
    public Exception Exception { get; internal set; }

    public static ApiResult WithMessage(string v)
    {
      return new ApiResult().WithMessage(v);
    }

    internal static ApiResult WithSecurityException()
    {
      return new ApiResult().WithSecurityException();
    }

    public static ApiResult Ok
    {
      get
      {
        return new ApiResult().WithOk();
      }
    }
  }

  public static class ApiResultUtil
  {
    public static ApiResult From(this ApiResult dest, ApiResult src)
    {
      dest.IsOk = src.IsOk;
      dest.Message = src.Message;
      dest.Exception = src.Exception;
      return dest;
    }

    public static ApiResult WithSecurityException(this ApiResult src)
    {
      src.Message = "Security Exception";
      src.Exception = new UnauthorizedAccessException();
      return src;
    }

    public static ApiResult WithMessage(this ApiResult src, string v)
    {
      src.Message = v;
      return src;
    }

    public static ApiResult WithOk(this ApiResult src)
    {
      src.IsOk = true;
      return src;
    }
  }
}
