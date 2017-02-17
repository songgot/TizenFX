/*
 * Copyright (c) 2016 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Tizen.Network.WiFi
{
    /// <summary>
    /// An abstract class for WiFi EAP information.
    /// </summary>
    internal interface IWiFiEap
    {
        /// <summary>
        /// The file path of CA Certificate of EAP.
        /// </summary>
        string CaCertificationFile { get; set; }

        /// <summary>
        /// The EAP type of wifi.
        /// </summary>
        WiFiEapType EapType { get; set; }

        /// <summary>
        /// The type of EAP phase2 authentication of Wi-Fi.
        /// </summary>
        WiFiAuthenticationType AuthenticationType { get; set; }
    } //WiFiEap
}
