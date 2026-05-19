import Footer from "./components/Footer"
import Header from "./components/Header"
import Profile from "./pages/Profile"
import Checkout from "./pages/Checkout"
import Home from "./pages/Home"
import Member from "./pages/Member"
import MovieList from "./pages/MovieList"
import MovieWithShowTimes from "./pages/MovieWithShowtimes"
import ShowtimeSeatSelection from "./pages/ShowtimeSeatSelection"
import PaymentResult from "./pages/PaymentResult"
import RetryPayment from "./pages/RetryPayment"
import Showtimes from "./pages/Showtimes"
import AuthCallback from "./pages/AuthCallback"
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-background text-on-background font-body selection:bg-secondary/30">
        <Header />
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/movies" element={<MovieList />} />
          <Route path="/movies/:movieId/showtimes" element={<MovieWithShowTimes />} />
          <Route path="/showtimes" element={<Showtimes />} />
          <Route path="/showtimes/:showtimeId/seats" element={<ShowtimeSeatSelection />} />
          <Route path="/checkout" element={<Checkout />} />
          <Route path="/payment-result" element={<PaymentResult />} />
          <Route path="/retry-payment" element={<RetryPayment />} />
          <Route path="/booking-history" element={<Navigate to="/profile" replace />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/member" element={<Member />} />
          <Route path="/auth-callback" element={<AuthCallback />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>

        <Footer />
      </div>
    </BrowserRouter>
  )
}

export default App
