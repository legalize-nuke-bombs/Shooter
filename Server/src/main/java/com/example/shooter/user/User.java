package com.example.shooter.user;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.Setter;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "users",
        indexes = {
                @Index(name = "idx_users_registeredAt", columnList = "registeredAt")
        }
)
@Getter
@Setter
@NoArgsConstructor
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, unique = true, length = UserConstants.USERNAME_MAX_LENGTH)
    private String username;

    @Column(nullable = false, length = UserConstants.DISPLAY_NAME_MAX_LENGTH)
    private String displayName;

    @Column(nullable = false)
    private Long registeredAt;

    @Column(nullable = false, length = UserConstants.BCRYPT_HASH_LENGTH)
    private String passwordHash;
}