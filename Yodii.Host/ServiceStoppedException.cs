#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Host\ServiceStoppedException.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Runtime.Serialization;

namespace Yodii.Host
{

    /// <summary>
    /// Exception raised whenever an event is raised by or a method is called on a stopped service. 
    /// </summary>
	[Serializable]
    public class ServiceStoppedException : ServiceNotAvailableException
	{
        /// <summary>
        /// Initializes a new <see cref="ServiceStoppedException"/>.
        /// </summary>
        /// <param name="serviceType">Type of the concerned service.</param>
		public ServiceStoppedException( Type serviceType )
            : base( serviceType )
		{
		}

        /// <summary>
        /// Initializes a new <see cref="ServiceStoppedException"/>.
        /// </summary>
        /// <param name="serviceType">Type of the concerned service.</param>
        /// <param name="message">Detailed message.</param>
        public ServiceStoppedException( Type serviceType, string message )
			: base( serviceType, message )
		{
        }

    }
}
