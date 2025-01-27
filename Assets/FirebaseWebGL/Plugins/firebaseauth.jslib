mergeInto(LibraryManager.library, {

    SignInAnonymously: function (objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            firebase.auth().signInAnonymously().then(function (result) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, "Success: signed up for " + result);
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },
    
    CreateUserWithEmailAndPassword: function (email, password, objectName, callback, fallback) {
        var parsedEmail = UTF8ToString(email);
        var parsedPassword = UTF8ToString(password);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {

            firebase.auth().createUserWithEmailAndPassword(parsedEmail, parsedPassword).then(function (test) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(test.user));
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    SignInWithEmailAndPassword: function (email, password, objectName, callback, fallback) {
        var parsedEmail = UTF8ToString(email);
        var parsedPassword = UTF8ToString(password);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {

            firebase.auth().signInWithEmailAndPassword(parsedEmail, parsedPassword).then(function (test) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(test.user));
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    SignInWithGoogle: function (objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            var provider = new firebase.auth.GoogleAuthProvider();
            firebase.auth().signInWithRedirect(provider).then(function (unused) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, "Success: signed in with Google!");
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    SignInWithFacebook: function (objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            var provider = new firebase.auth.FacebookAuthProvider();
            firebase.auth().signInWithRedirect(provider).then(function (unused) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, "Success: signed in with Facebook!");
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    SendEmailVerification: function(objectName, callback, fallback){
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            if(!firebase.auth().currentUser.emailVerified){
                firebase.auth().currentUser.sendEmailVerification().then(function (unused) {
                    window.unityInstance.SendMessage(parsedObjectName, parsedCallback, "Success: SendEmailVerification");
                }).catch(function (error) {
                    window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
                });
            }
        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    SendPasswordResetEmail: function(email, objectName, callback, fallback) {
        var parsedEmail = UTF8ToString(email);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        // // Configure the action code settings to match the exact payload
        // const actionCodeSettings = {
        //     url: 'https://intelliquest-3401f.web.app', // Add https:// protocol
        //     handleCodeInApp: true,
        //     continueUrl: 'https://intelliquest-3401f.web.app' // Full URL with protocol
        // };

        if (!parsedEmail) {
            const error = { code: "auth/missing-email", message: "Email is required." };
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error));
            return;
        }

        try {
            firebase.auth().sendPasswordResetEmail(parsedEmail)
                .then(() => {
                    console.log("Password reset email sent successfully to:", parsedEmail);
                    window.unityInstance.SendMessage(parsedObjectName, parsedCallback, 
                        "Success: SendPasswordResetEmail for " + parsedEmail);
                })
                .catch((error) => {
                    console.error("Failed to send password reset email:", error);
                    window.unityInstance.SendMessage(parsedObjectName, parsedFallback, 
                        JSON.stringify(error, Object.getOwnPropertyNames(error)));
                });
        } catch (error) {
            console.error("Exception in SendPasswordResetEmail:", error);
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, 
                JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },
    
    SignOut: function(objectName, callback, fallback){
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            firebase.auth().signOut().then(function (unused) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, "Success: SignOut");
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    OnAuthStateChanged: function (objectName, onUserSignedIn, onUserSignedOut) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedOnUserSignedIn = UTF8ToString(onUserSignedIn);
        var parsedOnUserSignedOut = UTF8ToString(onUserSignedOut);

        firebase.auth().onAuthStateChanged(function(user) {
            if (user) {
                window.unityInstance.SendMessage(parsedObjectName, parsedOnUserSignedIn, JSON.stringify(user));
            } else {
                window.unityInstance.SendMessage(parsedObjectName, parsedOnUserSignedOut, "User signed out");
            }
        });
    },

    ReloadFirebaseUser: function(objectName, callback, fallback){
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            firebase.auth().currentUser.reload().then(function (user) {
                window.unityInstance.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(firebase.auth().currentUser));
            }).catch(function (error) {
                window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            window.unityInstance.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },
});
