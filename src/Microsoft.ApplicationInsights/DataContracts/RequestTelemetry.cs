﻿namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics;

    /// <summary>
    /// Encapsulates information about a web request handled by the application.
    /// </summary>
    /// <remarks>
    /// You can send information about requests processed by your web application to Application Insights by
    /// passing an instance of the <see cref="RequestTelemetry"/> class to the <see cref="TelemetryClient.TrackRequest(RequestTelemetry)"/>
    /// method.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackrequest">Learn more</a>
    /// </remarks>
    public sealed class RequestTelemetry : OperationTelemetry, ITelemetry, ISupportProperties, ISupportMetrics, ISupportSampling
    {
        internal new const string TelemetryName = "Request";

        internal readonly string BaseType = typeof(RequestData).Name;
        internal readonly RequestData Data;
        private readonly TelemetryContext context;
        private bool successFieldSet;
        private IExtension extension;
        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class.
        /// </summary>
        public RequestTelemetry()
        {
            this.Data = new RequestData();
            this.context = new TelemetryContext(this.Data.properties);
            this.GenerateId();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class with the given <paramref name="name"/>,
        /// <paramref name="startTime"/>, <paramref name="duration"/>, <paramref name="responseCode"/> and <paramref name="success"/> property values.
        /// </summary>
        public RequestTelemetry(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
            : this()
        {
            this.Name = name; // Name is optional but without it UX does not make much sense
            this.Timestamp = startTime;
            this.Duration = duration;
            this.ResponseCode = responseCode;
            this.Success = success;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="RequestTelemetry"/> to clone from.</param>
        private RequestTelemetry(RequestTelemetry source)
        {
            this.Data = source.Data.DeepClone();
            this.context = source.context.DeepClone(this.Data.properties);
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
            this.successFieldSet = source.successFieldSet;
            this.extension = source.extension?.DeepClone();
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public override DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public override string Sequence { get; set; }

        /// <summary>
        /// Gets the object that contains contextual information about the application at the time when it handled the request.
        /// </summary>
        public override TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets gets the extension used to extend this telemetry instance using new strong typed object.
        /// </summary>
        public override IExtension Extension
        {
            get { return this.extension; }
            set { this.extension = value; }
        }

        /// <summary>
        /// Gets or sets Request ID.
        /// </summary>
        public override string Id
        {
            get { return this.Data.id; }
            set { this.Data.id = value; }
        }

        /// <summary>
        /// Gets or sets human-readable name of the requested page.
        /// </summary>
        public override string Name
        {
            get { return this.Data.name; }
            set { this.Data.name = value; }
        }

        /// <summary>
        /// Gets or sets response code returned by the application after handling the request.
        /// </summary>
        public string ResponseCode
        {
            get { return this.Data.responseCode; }
            set { this.Data.responseCode = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether application handled the request successfully.
        /// </summary>
        public override bool? Success
        {
            get
            {
                if (this.successFieldSet)
                {
                    return this.Data.success;
                }

                return null;
            }

            set
            {
                if (value != null && value.HasValue)
                {
                    this.Data.success = value.Value;
                    this.successFieldSet = true;
                }
                else
                {
                    this.Data.success = true;
                    this.successFieldSet = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the amount of time it took the application to handle the request.
        /// </summary>
        public override TimeSpan Duration
        {
            get { return this.Data.duration; }
            set { this.Data.duration = value; }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this request.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public override IDictionary<string, string> Properties
        {
            get
            {
                if (!this.Data.properties.ContainsKey(MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key))
                {
                    this.Data.properties[MetricTerms.Extraction.ProcessedByExtractors.Moniker.Key] = this.MetricExtractorInfo;
                }  
                
                return this.Data.properties;
            }
        }

        /// <summary>
        /// Gets or sets request url (optional).
        /// </summary>
        public Uri Url
        {
            get
            {
                if (this.Data.url.IsNullOrWhiteSpace())
                {
                    return null;
                }

                return new Uri(this.Data.url, UriKind.RelativeOrAbsolute);
            }

            set
            {
                this.Data.url = value?.ToString();
            }
        }

        /// <summary>
        /// Gets a dictionary of application-defined request metrics.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public override IDictionary<string, double> Metrics
        {
            get { return this.Data.measurements; }
        }

        /// <summary>
        /// Gets or sets the HTTP method of the request.
        /// </summary>
        [Obsolete("Include http verb into request telemetry name and use custom properties to report http method as a dimension.")]
        public string HttpMethod
        {
            get { return this.Properties["httpMethod"]; }
            set { this.Properties["httpMethod"] = value; }
        }

        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// </summary>
        double? ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }

        /// <summary>
        /// Gets or sets the source for the request telemetry object. This often is a hashed instrumentation key identifying the caller.
        /// </summary>
        public string Source
        {
            get { return this.Data.source; }
            set { this.Data.source = value; }
        }

        /// <summary>
        /// Gets or sets the MetricExtractorInfo.
        /// </summary>
        internal string MetricExtractorInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Deeply clones a <see cref="RequestTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public override ITelemetry DeepClone()
        {
            return new RequestTelemetry(this);
        }

        /// <inheritdoc/>
        public override void SerializeData(ISerializationWriter serializationWriter)
        {            
            serializationWriter.WriteProperty(this.Data);                        
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Properties.SanitizeProperties();
            this.Metrics.SanitizeMeasurements();
            this.Url = this.Url.SanitizeUri();

            // Set for backward compatibility:
            this.Data.id = this.Data.id.SanitizeName();
            this.Data.id = Utils.PopulateRequiredStringValue(this.Data.id, "id", typeof(RequestTelemetry).FullName);

            // Required fields
            if (!this.Success.HasValue)
            {
                this.Success = true;
            }

            if (string.IsNullOrEmpty(this.ResponseCode))
            {
                this.ResponseCode = this.Success.Value ? "200" : string.Empty;
            }           
        }
    }
}