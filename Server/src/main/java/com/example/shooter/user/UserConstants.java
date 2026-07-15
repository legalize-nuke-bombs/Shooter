package com.example.shooter.user;

public final class UserConstants {
    public static final int USERNAME_MIN_LENGTH = 4;
    public static final int USERNAME_MAX_LENGTH = 20;
    public static final String USERNAME_PATTERN = "^[a-zA-Z0-9_]+$";

    public static final int DISPLAY_NAME_MIN_LENGTH = 1;
    public static final int DISPLAY_NAME_MAX_LENGTH = 40;

    public static final int BCRYPT_HASH_LENGTH = 60;

    private UserConstants() {}
}
