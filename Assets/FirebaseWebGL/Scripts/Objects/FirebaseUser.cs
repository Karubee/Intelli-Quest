using System;
using System.Collections.Generic;

namespace FirebaseWebGL.Scripts.Objects
{
    [Serializable]
    public class FirebaseUser
    {
        public string userName;
        
        public string email;
        
        public bool isAnonymous;
        
        public bool emailVerified;
        
        public FirebaseUserMetadata metadata;
        
        public string phoneNumber;
        
        public FirebaseUserProvider[] providerData;
        
        public string providerId;
        
        public string uid;

    }
}