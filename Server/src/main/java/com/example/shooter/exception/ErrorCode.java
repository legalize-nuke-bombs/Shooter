package com.example.shooter.exception;

import lombok.Getter;
import org.springframework.http.HttpStatus;

import static org.springframework.http.HttpStatus.*;

@Getter
public enum ErrorCode {
    NOT_AUTHENTICATED(UNAUTHORIZED),
    INVALID_PASSWORD(FORBIDDEN),
    WEAK_PASSWORD(BAD_REQUEST),
    USERNAME_TAKEN(CONFLICT),
    USER_NOT_FOUND(NOT_FOUND),
    EMPTY_REQUEST(BAD_REQUEST),

    WORLD_NOT_FOUND(NOT_FOUND),
    WORLD_DOES_NOT_ACCEPT_NEW_MEMBERS(FORBIDDEN),
    NOT_A_MEMBER(BAD_REQUEST),
    PLAYER_NOT_FOUND(NOT_FOUND),
    CANT_SELF_KICK(BAD_REQUEST),
    CANT_KICK_THIS_USER(FORBIDDEN),
    NOT_A_CREATOR(FORBIDDEN),
    CANT_SELF_BLACKLIST(BAD_REQUEST),
    ALREADY_BLACKLISTED(BAD_REQUEST),
    NOT_BLACKLISTED(BAD_REQUEST),
    BLACKLISTED(FORBIDDEN),

    MALFORMED_REQUEST(BAD_REQUEST),
    INTERNAL_ERROR(INTERNAL_SERVER_ERROR);

    final HttpStatus status;

    ErrorCode(HttpStatus status) {
        this.status = status;
    }
}
