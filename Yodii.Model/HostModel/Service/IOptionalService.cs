﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yodii.Model.HostModel.Service
{
    public interface IOptionalService<T> : IService<T> where T : IYodiiService
    {
    }
}
