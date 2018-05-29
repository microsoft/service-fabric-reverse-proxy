using System;

namespace managementserver.subscriber
{
    public sealed class ResponseSubscriber : AsyncSubscriber<Response>
    {
        private Action<Response> ResponseAction;
        public ResponseSubscriber(Action<Response> action)
        {
            ResponseAction = action;
        }
        protected override bool WhenNext(Response element)
        {
            ResponseAction(element);
            return true;
        }
    }
}
