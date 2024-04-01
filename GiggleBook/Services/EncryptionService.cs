using Microsoft.AspNetCore.Authentication;
using System.Security.Cryptography;

namespace GiggleBook.Services;

public static class EncryptionService
{
    public static string EncryptPassword(string password)
    {
        var alg = new HMACSHA256(Base64UrlTextEncoder.Decode("AAAAB3NzaC1yc2EAAAADAQABAAACAQCuMs2Y+tgo6h3aYWLqBFuAZ7IxzJbLcl/JHeV41y+oXaha7N/QvZdJl9zX3ZTcsJq/I3Z/dH9Ilb8Frh1DsrZsHW2fsjIrIt23SvkWEVnmiA4rKy5vQBbkgrj2eTzMSgnRWl2qf9kYwBRF/2cujMiEgP6mVpiMAISqzUgbtbfwXkcOn1H1OHtPOvNhUHe/plZWuIxTYFHCfGZWR1DPTKLo6WitP6SO7c93wOcL14+ffMtK+weQJhVxb49Ff+b+tyqZeBB+gaZI46JMpkO5cZmJT6D7ME6CMMuD0ioX8aGecnSSUlVHdnGLoms1qWLQ47OGEqGfWdL0FOVDEa2+Q1wW46DFHjWa7uV8iXEq1Jk08FJl65I9Irsy8l0NYLtRrFEK4PxIk5IzcgeHz72q0lvHQSzi1NXliGPdVJqC6TdfKM5YS+5694WdUrcQN/FNRvZhJY50catGfNaszPxaY06jP+gALn+SUvAWXsXvpS15ezZEr7HaaEN5DKfuxEkNXohietyBS4WX75r19gdrc2g4Fzh4a/XbC6ezOcHqa9FjX3jmyY6ZzEE8D7FVZDZk5nNczUSOxOlxHb4NBlu5NlRLvZ/5BMRbQyTzuzZyVRuLU/13rcHsH7rOQFE6bSgStDtGAXufCB6Pw0iyvF7O3d1m6qHgrwC+vaPXNbWqQzJ0xQ=="));

        return Base64UrlTextEncoder.Encode(alg.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
    }
}
