﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yodii.Model
{
    public interface IRunningService<T> : IService<T> where T : IYodiiService
    {
    }
}