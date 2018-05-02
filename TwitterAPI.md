# Twitter API Document

## Note
1. All parameters will be passed through x-www-form-urlencoded *FIXME this should be implemented in JSON payload*
2. Session is maintained in the HTTP header *FIXME this should be cookie*

## User
### URL: /twitter/user/
**POST**: create new user

- Required: None
- Parameters: username and password
- Return status:
 - 201: user added
 - 500: adding error
 - 400: username/password missing

### URL: /twitterapi/login
**POST**: login into an account

- Required: None
- Parameters: username and password
- Return status:
 - 201: login successfully, return {"Session": sessionValue}
 - 500: can't create session
 - 404: invalid username/password
 - 400: username/password missing

### URL: /twitterapi/logout
**POST**: logout from an account

- Required: a valid session
- Parameters: None
- Return status:
 - 200: logout successfully
 - 500: logout error

## Tweet
### URL: /twitter/
**GET**: get following timeline

- Required: a valid session
- Parameters: None
- Return status:
 - 200: timeline avaliable, return [ {"TwitterId": id, "Message": Message Text, "User": username, "DateCreated": timestamp} ]
 - 404: no post in timeline
 - 500: internal error

### URL: /twitter/tweet/
**GET**: get list of user's tweets (i.e., timeline)

- Required: a valid session
- Parameters: None
- Return status:
 - 200: timeline avaliable, return [ {"TwitterId": id, "Message": Message Text, "User": username, "DateCreated": timestamp} ]
 - 404: no post in timeline
 - 500: internal error

**POST**: post new tweet

- Required: a valid session
- Parameters: message
- Return status:
 - 201: post successfully
 - 400: message missing
 - 500: internal error

**DELETE**: delete a tweet *FIXME not implemented yet*

## Following
### URL: /twitter/following/
**GET**: get list of user's following

- Required: a valid session
- Parameters: None
- Return status:
 - 200: following list, return [ {"FollowingId": id, "Name": username} ]
 - 404: no following

**POST**: follow a user

- Required: a valid session
- Parameters: followingname
- Return status:
 - 201: following successfully, return [ {"FollowingId": id, "Name": username} ]
 - 404: no following
 - 500: can't add following

**DELETE**: unfollow a user

- Required: a valid session
- Parameters: followingname
- Return status:
 - 200: unfollowing successfully, return [ {"FollowingId": id, "Name": username} ]
 - 404: no following
 - 502: can't remove following