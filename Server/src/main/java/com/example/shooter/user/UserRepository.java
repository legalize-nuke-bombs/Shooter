package com.example.shooter.user;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import java.util.Optional;

public interface UserRepository extends JpaRepository<User, Long> {
    @Query("SELECT u FROM User u WHERE u.username = ?1")
    Optional<User> findByUsername(String username);

    @Query("SELECT COUNT(u) FROM User u")
    long countAll();

    @Query("SELECT COUNT(u) FROM User u WHERE u.registeredAt >= ?1")
    long countSince(long timestamp);
}